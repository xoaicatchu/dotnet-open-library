using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using StockFlow.Api.Data;
using StockFlow.Tests.Infrastructure;

namespace StockFlow.Tests;
[Collection("Postgres")]
public sealed class ProductionTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Production_does_not_seed_demo_accounts_even_if_seed_password_is_configured()
    {
        var path = Path.Combine(Path.GetTempPath(), $"stockflow-{Guid.NewGuid():N}.pfx");
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=StockFlow tests", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        await File.WriteAllBytesAsync(path, cert.Export(X509ContentType.Pfx));
        try
        {
            await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString(), "Production", new()
            {
                ["DataProtection:CertificatePath"] = path,
                ["DevelopmentSeed:Password"] = $"Strong!{Guid.NewGuid():N}"
            });
            var client = factory.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
            Assert.False(await db.Users.AnyAsync(x => x.Email == "admin@stockflow.local" || x.Email == "warehouse@stockflow.local" || x.Email == "viewer@stockflow.local"));
            var key = scope.ServiceProvider.GetRequiredService<IKeyManager>().CreateNewKey(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90));
            Assert.True(await db.DataProtectionKeys.AnyAsync(x => x.Xml != null && x.Xml.Contains(key.KeyId.ToString()) && x.Xml.Contains("encryptedSecret")));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Bootstrap_command_without_operator_secret_exits_nonzero_without_starting_api()
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false };
        start.ArgumentList.Add(typeof(Program).Assembly.Location); start.ArgumentList.Add("--bootstrap-admin");
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["ConnectionStrings__StockFlow"] = postgres.Database.GetConnectionString();
        start.Environment.Remove("Bootstrap__Email"); start.Environment.Remove("Bootstrap__Password");
        using var process = Process.Start(start)!;
        var errorTask = process.StandardError.ReadToEndAsync(); var outputTask = process.StandardOutput.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); } catch { process.Kill(true); throw; }
        Assert.NotEqual(0, process.ExitCode);
        Assert.Contains("Bootstrap:Email", await errorTask);
        Assert.DoesNotContain("Now listening", await outputTask);
    }

    [Fact]
    public async Task Spa_deep_links_serve_shell_while_unknown_api_stays_404()
    {
        var root = Path.Combine(Path.GetTempPath(), $"stockflow-web-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root); await File.WriteAllTextAsync(Path.Combine(root, "index.html"), "<html>StockFlow shell</html>");
        try
        {
            await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString(), webRoot: root);
            var client = factory.CreateClient();
            var deep = await client.GetAsync("/documents/a-new-draft");
            Assert.Equal(HttpStatusCode.OK, deep.StatusCode); Assert.Contains("StockFlow shell", await deep.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/unknown")).StatusCode);
        }
        finally { Directory.Delete(root, true); }
    }
}

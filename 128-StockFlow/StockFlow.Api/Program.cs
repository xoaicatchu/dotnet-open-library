using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Auth;
using StockFlow.Api.Data;
using StockFlow.Api.Documents;
using StockFlow.Api.Inventory;
using StockFlow.Api.Sync;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<StockFlowDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("StockFlow") ?? throw new InvalidOperationException("ConnectionStrings:StockFlow is required."),
    postgres => postgres.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null)));
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 12;
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<StockFlowDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "StockFlow.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing") ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
var protection = builder.Services.AddDataProtection().SetApplicationName("StockFlow").PersistKeysToDbContext<StockFlowDbContext>();
if (builder.Environment.IsProduction() && !args.Contains("--migrate") && !args.Contains("--bootstrap-admin"))
{
    var certPath = builder.Configuration["DataProtection:CertificatePath"];
    if (string.IsNullOrWhiteSpace(certPath)) throw new InvalidOperationException("Production requires DataProtection:CertificatePath from a mounted Secret.");
    protection.ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile(certPath, builder.Configuration["DataProtection:CertificatePassword"]));
}
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "StockFlow.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing") ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAuthorization(options => options.AddPolicy(RolePolicies.WriteInventory, policy => policy.RequireRole(RolePolicies.Admin, RolePolicies.Warehouse)));
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<InventoryTransactionRunner>();
builder.Services.AddScoped<StockMovementWriter>();
builder.Services.AddScoped<StockDocumentService>();
builder.Services.AddScoped<ClientOperationStore>();
builder.Services.AddScoped<SyncService>();
var app = builder.Build();
if (args.Contains("--bootstrap-admin"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await DevelopmentSeeder.BootstrapAdminAsync(scope.ServiceProvider, app.Configuration);
    return;
}
if (args.Contains("--migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<StockFlowDbContext>().Database.MigrateAsync();
    await DevelopmentSeeder.EnsureRolesAsync(scope.ServiceProvider);
    return;
}
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await DevelopmentSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}
app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Map("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program;

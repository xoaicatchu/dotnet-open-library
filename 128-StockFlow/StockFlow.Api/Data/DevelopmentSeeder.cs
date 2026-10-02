using Microsoft.AspNetCore.Identity;
using StockFlow.Api.Auth;
namespace StockFlow.Api.Data;
public static class DevelopmentSeeder
{
    public static async Task EnsureRolesAsync(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var name in RolePolicies.Roles)
            if (!await roles.RoleExistsAsync(name) && !(await roles.CreateAsync(new(name))).Succeeded)
                throw new InvalidOperationException("Không thể tạo vai trò Identity.");
    }
    public static async Task BootstrapAdminAsync(IServiceProvider services, IConfiguration config)
    {
        var email = config["Bootstrap:Email"];
        var password = config["Bootstrap:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Bootstrap:Email và Bootstrap:Password phải được cung cấp ngoài image.");
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        if (await users.FindByEmailAsync(email) is not null) throw new InvalidOperationException("Bootstrap từ chối ghi đè tài khoản đã tồn tại.");
        await EnsureRolesAsync(services);
        await CreateAsync(users, email, password, RolePolicies.Admin);
    }
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        await EnsureRolesAsync(services);
        var password = config["DevelopmentSeed:Password"];
        if (string.IsNullOrWhiteSpace(password)) return;
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        foreach (var role in RolePolicies.Roles)
        {
            var email = $"{role.ToLowerInvariant()}@stockflow.local";
            if (await users.FindByEmailAsync(email) is null) await CreateAsync(users, email, password, role);
        }
    }
    private static async Task CreateAsync(UserManager<IdentityUser> users, string email, string password, string role)
    {
        var user = new IdentityUser { UserName = email.Trim(), Email = email.Trim() };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        result = await users.AddToRoleAsync(user, role);
        if (!result.Succeeded) { await users.DeleteAsync(user); throw new InvalidOperationException("Không thể gán vai trò."); }
    }
}

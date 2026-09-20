using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace InventoryIdServer.Api.Configuration;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email()
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new List<ApiScope>
        {
            new(IdentityServerConstants.LocalApi.ScopeName, "IdentityServer Local API"),
            new("inventory_api", "Inventory Management API Scope")
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new List<ApiResource>
        {
            new("inventory_api", "Inventory API")
            {
                Scopes = { "inventory_api", IdentityServerConstants.LocalApi.ScopeName }
            }
        };

    public static IEnumerable<Client> Clients =>
        new List<Client>
        {
            new()
            {
                ClientId = "machine_worker",
                ClientName = "Internal Background Worker",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("worker_secret_key_123".Sha256()) },
                AllowedScopes =
                {
                    IdentityServerConstants.LocalApi.ScopeName,
                    "inventory_api"
                }
            }
        };
}

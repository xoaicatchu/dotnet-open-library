using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthServerOpenIddict.Api.Controllers;

[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager;

    public AuthorizationController(IOpenIddictApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    /// <summary>
    /// OAuth 2.0 Token endpoint supporting Client Credentials flow.
    /// </summary>
    [HttpPost("~/connect/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidRequest,
                ErrorDescription = "The OpenID Connect request cannot be retrieved."
            });
        }

        if (request.IsClientCredentialsGrantType())
        {
            var application = await _applicationManager.FindByClientIdAsync(request.ClientId ?? string.Empty);
            if (application == null)
            {
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.InvalidClient,
                    ErrorDescription = "The client application was not found."
                });
            }

            var identity = new ClaimsIdentity(
                authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            // Add subject and name claims
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, request.ClientId!));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, await _applicationManager.GetDisplayNameAsync(application) ?? request.ClientId!));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, "ServiceWorker"));

            // Set scopes
            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());

            // Set claim destinations so they are included in the access token
            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = OpenIddictConstants.Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });
    }
}

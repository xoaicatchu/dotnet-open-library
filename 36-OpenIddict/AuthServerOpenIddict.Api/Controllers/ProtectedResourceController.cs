using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using AuthServerOpenIddict.Api.Models;

namespace AuthServerOpenIddict.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class ProtectedResourceController : ControllerBase
{
    /// <summary>
    /// Returns protected confidential data accessible only with a valid access token.
    /// </summary>
    [HttpGet("secret-data")]
    [ProducesResponseType(typeof(SecretDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetSecretData()
    {
        var clientId = User.FindFirstValue(OpenIddictConstants.Claims.Subject) ?? "Unknown";
        return Ok(new SecretDataResponse(
            SecretMessage: "Highly confidential enterprise data protected by OpenIddict.",
            ClientId: clientId,
            Timestamp: DateTime.UtcNow
        ));
    }

    /// <summary>
    /// Returns the authenticated client's identity and claims.
    /// </summary>
    [HttpGet("user-info")]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetUserInfo()
    {
        var clientId = User.FindFirstValue(OpenIddictConstants.Claims.Subject) ?? "Unknown";
        var scopes = User.FindAll(OpenIddictConstants.Claims.Scope)
            .Concat(User.FindAll(OpenIddictConstants.Claims.Private.Scope))
            .Concat(User.FindAll("scope"))
            .Select(c => c.Value)
            .Distinct()
            .ToList();
        var roles = User.FindAll(ClaimTypes.Role)
            .Concat(User.FindAll(OpenIddictConstants.Claims.Role))
            .Concat(User.FindAll("role"))
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        return Ok(new UserInfoResponse(
            ClientId: clientId,
            Scopes: scopes,
            Roles: roles
        ));
    }
}

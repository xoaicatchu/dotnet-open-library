namespace AuthServerOpenIddict.Api.Models;

public record SecretDataResponse(
    string SecretMessage,
    string ClientId,
    DateTime Timestamp
);

public record UserInfoResponse(
    string ClientId,
    List<string> Scopes,
    List<string> Roles
);

public record TokenResponse(
    string access_token,
    string token_type,
    int expires_in
);

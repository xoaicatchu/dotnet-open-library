namespace AuditLogNLog.Api.Models;

public record LoginAuditRequest(
    string Username,
    string IpAddress,
    bool Success
);

public record ActionAuditRequest(
    string Username,
    string ActionName,
    string ResourceId,
    string Details
);

public record AuditResponse(
    string EventId,
    string Status,
    DateTime Timestamp
);

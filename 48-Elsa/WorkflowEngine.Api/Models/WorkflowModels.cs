namespace WorkflowEngine.Api.Models;

public record WorkflowDefinitionDto(
    string Id,
    string Name,
    string Description,
    int Version,
    bool IsPublished,
    DateTime CreatedAt);

public record WorkflowInstanceDto(
    string Id,
    string DefinitionId,
    string WorkflowName,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    Dictionary<string, object?> Output);

public record RunWorkflowRequest(string? CorrelationId, Dictionary<string, object>? Input);

public record RunWorkflowResponse(string WorkflowInstanceId, string Status);

public record OrderApprovalInput(string OrderId, decimal Amount, string RequestedBy);

public record OrderApprovalResult(string OrderId, bool IsApproved, string Reason, DateTime ProcessedAt);

namespace DynamicRuleEngine.Api.Domain.Models;

/// <summary>
/// Kết quả đánh giá của một Rule cụ thể.
/// </summary>
public record RuleResult(
    string RuleId,
    string RuleName,
    bool IsSuccess,
    string? ErrorMessage = null,
    long ExecutionTimeUs = 0, // Microseconds
    Dictionary<string, object?>? Outputs = null
);

/// <summary>
/// Kết quả tổng thể khi đánh giá một Workflow gồm nhiều Rule.
/// </summary>
public record WorkflowExecutionResult(
    string WorkflowId,
    bool IsSuccess,
    long TotalExecutionTimeUs,
    int ExecutedRulesCount,
    int PassedRulesCount,
    Dictionary<string, object?> Outputs,
    List<RuleResult> RuleResults
);

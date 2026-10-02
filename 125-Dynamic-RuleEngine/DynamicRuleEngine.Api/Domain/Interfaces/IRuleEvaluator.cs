using DynamicRuleEngine.Api.Domain.Models;

namespace DynamicRuleEngine.Api.Domain.Interfaces;

/// <summary>
/// Dịch vụ đánh giá dữ liệu đầu vào dựa trên tập luật của một Workflow.
/// </summary>
public interface IRuleEvaluator
{
    Task<WorkflowExecutionResult> EvaluateWorkflowAsync<T>(
        string workflowId, 
        T input, 
        CancellationToken ct = default);
}

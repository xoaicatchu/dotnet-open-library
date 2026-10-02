using DynamicRuleEngine.Api.Domain.Models;

namespace DynamicRuleEngine.Api.Domain.Interfaces;

/// <summary>
/// Kho lưu trữ và quản lý các Workflow và Rules trong hệ thống.
/// </summary>
public interface IWorkflowRepository
{
    Task<IReadOnlyList<Workflow>> GetAllWorkflowsAsync(CancellationToken ct = default);
    Task<Workflow?> GetWorkflowAsync(string workflowId, CancellationToken ct = default);
    Task SaveWorkflowAsync(Workflow workflow, CancellationToken ct = default);
    Task<bool> DeleteWorkflowAsync(string workflowId, CancellationToken ct = default);
    Task<bool> ToggleRuleStatusAsync(string workflowId, string ruleId, bool isActive, CancellationToken ct = default);
}

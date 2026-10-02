using System.Diagnostics;
using DynamicRuleEngine.Api.Domain.Interfaces;
using DynamicRuleEngine.Api.Domain.Models;

namespace DynamicRuleEngine.Api.Infrastructure.Services;

/// <summary>
/// Động cơ thực thi và chấm điểm các quy tắc nghiệp vụ với khả năng đo đạc chi tiết (Tracing).
/// </summary>
public class RuleEvaluatorService : IRuleEvaluator
{
    private readonly IWorkflowRepository _workflowRepo;
    private readonly IRuleCompiler _compiler;
    private readonly ILogger<RuleEvaluatorService> _logger;

    public RuleEvaluatorService(
        IWorkflowRepository workflowRepo,
        IRuleCompiler compiler,
        ILogger<RuleEvaluatorService> logger)
    {
        _workflowRepo = workflowRepo;
        _compiler = compiler;
        _logger = logger;
    }

    public async Task<WorkflowExecutionResult> EvaluateWorkflowAsync<T>(
        string workflowId,
        T input,
        CancellationToken ct = default)
    {
        var totalSw = Stopwatch.StartNew();

        var workflow = await _workflowRepo.GetWorkflowAsync(workflowId, ct);
        if (workflow == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Workflow với ID '{workflowId}'.");
        }

        if (!workflow.IsActive)
        {
            throw new InvalidOperationException($"Workflow '{workflow.WorkflowName}' ({workflowId}) hiện đang bị tạm khóa.");
        }

        var activeRules = workflow.Rules
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ToList();

        var ruleResults = new List<RuleResult>();
        var aggregatedOutputs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        int passedCount = 0;

        foreach (var rule in activeRules)
        {
            var ruleSw = Stopwatch.StartNew();
            bool isMatch = false;
            string? error = null;

            try
            {
                var compiledFunc = _compiler.Compile<T>(rule.Expression);
                isMatch = compiledFunc(input);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                _logger.LogWarning(ex, "Lỗi khi đánh giá Rule '{RuleName}' trong Workflow '{WorkflowId}'", rule.RuleName, workflowId);
            }
            finally
            {
                ruleSw.Stop();
            }

            var microsecond = (long)(ruleSw.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
            var ruleOutputs = new Dictionary<string, object?>();

            if (isMatch)
            {
                passedCount++;
                foreach (var action in rule.SuccessActions)
                {
                    ruleOutputs[action.OutputKey] = action.OutputValue;
                    aggregatedOutputs[action.OutputKey] = action.OutputValue;
                }
            }
            else if (error == null)
            {
                foreach (var action in rule.FailureActions)
                {
                    ruleOutputs[action.OutputKey] = action.OutputValue;
                    aggregatedOutputs[action.OutputKey] = action.OutputValue;
                }
            }

            ruleResults.Add(new RuleResult(
                rule.RuleId,
                rule.RuleName,
                isMatch,
                error,
                microsecond,
                ruleOutputs
            ));

            // Nếu luật này thỏa mãn và có cờ StopOnSuccess -> Kết thúc chuỗi kiểm tra
            if (isMatch && rule.StopOnSuccess)
            {
                _logger.LogDebug("Rule '{RuleName}' kích hoạt cờ StopOnSuccess. Dừng duyệt các Rule tiếp theo.", rule.RuleName);
                break;
            }
        }

        totalSw.Stop();
        var totalMicrosecond = (long)(totalSw.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));

        return new WorkflowExecutionResult(
            WorkflowId: workflowId,
            IsSuccess: passedCount > 0,
            TotalExecutionTimeUs: totalMicrosecond,
            ExecutedRulesCount: ruleResults.Count,
            PassedRulesCount: passedCount,
            Outputs: aggregatedOutputs,
            RuleResults: ruleResults
        );
    }
}

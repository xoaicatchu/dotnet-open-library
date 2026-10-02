namespace DynamicRuleEngine.Api.Domain.Models;

/// <summary>
/// Hành động hoặc giá trị đầu ra được sinh ra khi một Rule đánh giá thành công hoặc thất bại.
/// Ví dụ: Key = "CoveragePercent", Value = 80
/// </summary>
public record RuleAction(
    string Name,
    string OutputKey,
    object OutputValue
);

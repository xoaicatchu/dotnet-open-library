namespace DynamicRuleEngine.Api.Domain.Models;

/// <summary>
/// Đại diện cho một tập hợp các quy tắc liên quan cùng giải quyết một bài toán nghiệp vụ.
/// Ví dụ: Workflow BHYT, Workflow Chiết khấu Giỏ hàng, Workflow Đánh giá Tín dụng.
/// </summary>
public class Workflow
{
    public string WorkflowId { get; set; } = default!;
    public string WorkflowName { get; set; } = default!;
    public string TargetTypeName { get; set; } = default!; // Kiểu dữ liệu ngữ cảnh (ví dụ: "MedicalInsuranceContext")
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Rule> Rules { get; set; } = new();
}

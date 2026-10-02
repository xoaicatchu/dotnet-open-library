namespace DynamicRuleEngine.Api.Domain.Models;

/// <summary>
/// Định nghĩa một luật nghiệp vụ (Rule) độc lập có thể cấu hình động.
/// </summary>
public class Rule
{
    public string RuleId { get; set; } = Guid.NewGuid().ToString("N");
    public string RuleName { get; set; } = default!;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Biểu thức logic được viết dưới dạng Dynamic C# Expression.
    /// Ví dụ: "Age >= 60 and IsRightRoute == true"
    /// hoặc "DiagnosisCode.StartsWith(\"I10\")"
    /// </summary>
    public string Expression { get; set; } = default!;

    /// <summary>
    /// Thứ tự ưu tiên thực thi (Số càng nhỏ chạy trước).
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Trạng thái bật/tắt của luật. Nếu false sẽ bỏ qua khi đánh giá.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Nếu true và Rule này thỏa mãn -> Dừng toàn bộ Workflow, không duyệt các Rule sau.
    /// </summary>
    public bool StopOnSuccess { get; set; } = false;

    /// <summary>
    /// Danh sách các giá trị output khi Rule đánh giá đạt (True).
    /// </summary>
    public List<RuleAction> SuccessActions { get; set; } = new();

    /// <summary>
    /// Danh sách các giá trị output khi Rule đánh giá không đạt (False).
    /// </summary>
    public List<RuleAction> FailureActions { get; set; } = new();
}

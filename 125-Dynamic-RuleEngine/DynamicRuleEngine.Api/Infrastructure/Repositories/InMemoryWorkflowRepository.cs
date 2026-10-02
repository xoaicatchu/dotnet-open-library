using System.Collections.Concurrent;
using DynamicRuleEngine.Api.Domain.Interfaces;
using DynamicRuleEngine.Api.Domain.Models;
using DynamicRuleEngine.Api.Domain.UseCases;

namespace DynamicRuleEngine.Api.Infrastructure.Repositories;

/// <summary>
/// Quản lý danh mục Workflow & Rules trong bộ nhớ với khả năng cập nhật Hot-Reload không cần restart.
/// Có nạp sẵn các bộ luật thực tế (BHYT và Chiết khấu E-Commerce).
/// </summary>
public class InMemoryWorkflowRepository : IWorkflowRepository
{
    private readonly ConcurrentDictionary<string, Workflow> _workflows = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryWorkflowRepository()
    {
        SeedDefaultWorkflows();
    }

    private void SeedDefaultWorkflows()
    {
        // 1. Workflow Đánh giá Mức hưởng Bảo hiểm Y tế (HIS / BHYT)
        var bhytWorkflow = new Workflow
        {
            WorkflowId = "medical-insurance-approval",
            WorkflowName = "Quy trình Đánh giá Tỷ lệ Hưởng BHYT Viện Phí",
            TargetTypeName = nameof(MedicalInsuranceContext),
            Description = "Tự động phân loại tỷ lệ thanh toán BHYT theo Luật Khám chữa bệnh hiện hành",
            IsActive = true,
            UpdatedAtUtc = DateTime.UtcNow,
            Rules = new List<Rule>
            {
                new Rule
                {
                    RuleId = "bhyt-01-tre-em",
                    RuleName = "Trẻ em dưới 6 tuổi",
                    Description = "Trẻ em <= 6 tuổi được thanh toán 100% không phụ thuộc tuyến",
                    Expression = "Age <= 6",
                    Priority = 1,
                    IsActive = true,
                    StopOnSuccess = true,
                    SuccessActions = new List<RuleAction>
                    {
                        new("SetCoverage", "CoveragePercent", 100),
                        new("SetReason", "Reason", "Đối tượng trẻ em dưới 6 tuổi được nhà nước chi trả 100%"),
                        new("SetStatus", "ApprovalStatus", "Approved")
                    }
                },
                new Rule
                {
                    RuleId = "bhyt-02-man-tinh-cao-tuoi",
                    RuleName = "Người cao tuổi mắc bệnh mãn tính đúng tuyến",
                    Description = "Bệnh nhân từ 60 tuổi trở lên có bệnh mãn tính và đi đúng tuyến",
                    Expression = "Age >= 60 and HasChronicDisease == true and IsRightRoute == true",
                    Priority = 2,
                    IsActive = true,
                    StopOnSuccess = true,
                    SuccessActions = new List<RuleAction>
                    {
                        new("SetCoverage", "CoveragePercent", 95),
                        new("PriorityCare", "IsPriorityCare", true),
                        new("SetReason", "Reason", "Người cao tuổi bệnh mãn tính đúng tuyến hưởng 95%"),
                        new("SetStatus", "ApprovalStatus", "Approved")
                    }
                },
                new Rule
                {
                    RuleId = "bhyt-03-dung-tuyen-chuan",
                    RuleName = "Khám chữa bệnh đúng tuyến chuẩn",
                    Description = "Bệnh nhân đi khám đúng tuyến đăng ký ban đầu",
                    Expression = "IsRightRoute == true",
                    Priority = 3,
                    IsActive = true,
                    StopOnSuccess = true,
                    SuccessActions = new List<RuleAction>
                    {
                        new("SetCoverage", "CoveragePercent", 80),
                        new("SetReason", "Reason", "Khám chữa bệnh đúng tuyến thông thường hưởng 80%"),
                        new("SetStatus", "ApprovalStatus", "Approved")
                    }
                },
                new Rule
                {
                    RuleId = "bhyt-04-trai-tuyen",
                    RuleName = "Khám vượt tuyến / Trái tuyến ngoại trú",
                    Description = "Bệnh nhân tự đi khám trái tuyến tại bệnh viện tuyến tỉnh/trung ương",
                    Expression = "IsRightRoute == false",
                    Priority = 4,
                    IsActive = true,
                    StopOnSuccess = true,
                    SuccessActions = new List<RuleAction>
                    {
                        new("SetCoverage", "CoveragePercent", 40),
                        new("SetReason", "Reason", "Khám chữa bệnh trái tuyến chỉ được chi trả 40% chi phí"),
                        new("SetStatus", "ApprovalStatus", "ApprovedWithCondition")
                    }
                }
            }
        };

        // 2. Workflow Chiết khấu Giỏ hàng E-Commerce
        var discountWorkflow = new Workflow
        {
            WorkflowId = "order-discount-rules",
            WorkflowName = "Chính sách Chiết khấu Bán hàng Động",
            TargetTypeName = nameof(OrderDiscountContext),
            Description = "Áp dụng giảm giá bậc thang và ưu đãi vận chuyển theo hạng khách hàng",
            IsActive = true,
            UpdatedAtUtc = DateTime.UtcNow,
            Rules = new List<Rule>
            {
                new Rule
                {
                    RuleId = "disc-01-diamond-vip",
                    RuleName = "Khách hàng Diamond đơn lớn",
                    Expression = "CustomerTier == \"Diamond\" and TotalOrderAmount >= 2000000",
                    Priority = 1,
                    IsActive = true,
                    SuccessActions = new List<RuleAction>
                    {
                        new("SetDiscount", "DiscountPercent", 20),
                        new("FreeShipping", "FreeShipping", true)
                    }
                },
                new Rule
                {
                    RuleId = "disc-02-weekend-gold",
                    RuleName = "Khách hàng Gold mua hàng cuối tuần",
                    Expression = "CustomerTier == \"Gold\" and IsWeekend == true",
                    Priority = 2,
                    IsActive = true,
                    SuccessActions = new List<RuleAction>
                    {
                        new("SetDiscount", "DiscountPercent", 15)
                    }
                },
                new Rule
                {
                    RuleId = "disc-03-bulk-purchase",
                    RuleName = "Mua số lượng lớn trên 10 sản phẩm",
                    Expression = "ItemCount >= 10",
                    Priority = 3,
                    IsActive = true,
                    SuccessActions = new List<RuleAction>
                    {
                        new("ExtraGift", "HasFreeGift", true)
                    }
                }
            }
        };

        _workflows[bhytWorkflow.WorkflowId] = bhytWorkflow;
        _workflows[discountWorkflow.WorkflowId] = discountWorkflow;
    }

    public Task<IReadOnlyList<Workflow>> GetAllWorkflowsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Workflow> list = _workflows.Values.OrderBy(w => w.WorkflowName).ToList();
        return Task.FromResult(list);
    }

    public Task<Workflow?> GetWorkflowAsync(string workflowId, CancellationToken ct = default)
    {
        _workflows.TryGetValue(workflowId, out var workflow);
        return Task.FromResult(workflow);
    }

    public Task SaveWorkflowAsync(Workflow workflow, CancellationToken ct = default)
    {
        workflow.UpdatedAtUtc = DateTime.UtcNow;
        _workflows[workflow.WorkflowId] = workflow;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteWorkflowAsync(string workflowId, CancellationToken ct = default)
    {
        return Task.FromResult(_workflows.TryRemove(workflowId, out _));
    }

    public Task<bool> ToggleRuleStatusAsync(string workflowId, string ruleId, bool isActive, CancellationToken ct = default)
    {
        if (_workflows.TryGetValue(workflowId, out var workflow))
        {
            var rule = workflow.Rules.FirstOrDefault(r => r.RuleId == ruleId);
            if (rule != null)
            {
                rule.IsActive = isActive;
                workflow.UpdatedAtUtc = DateTime.UtcNow;
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }
}

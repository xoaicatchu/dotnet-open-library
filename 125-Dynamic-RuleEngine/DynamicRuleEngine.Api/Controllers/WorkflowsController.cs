using Microsoft.AspNetCore.Mvc;
using DynamicRuleEngine.Api.Domain.Interfaces;
using DynamicRuleEngine.Api.Domain.Models;
using DynamicRuleEngine.Api.Domain.UseCases;

namespace DynamicRuleEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowRepository _workflowRepo;
    private readonly IRuleCompiler _compiler;

    public WorkflowsController(IWorkflowRepository workflowRepo, IRuleCompiler compiler)
    {
        _workflowRepo = workflowRepo;
        _compiler = compiler;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các Workflow và tập luật đi kèm.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Workflow>>> GetAll()
    {
        var list = await _workflowRepo.GetAllWorkflowsAsync();
        return Ok(list);
    }

    /// <summary>
    /// Lấy chi tiết một Workflow cụ thể.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Workflow>> GetById(string id)
    {
        var workflow = await _workflowRepo.GetWorkflowAsync(id);
        if (workflow == null)
        {
            return NotFound(new { error = $"Không tìm thấy Workflow với ID '{id}'." });
        }
        return Ok(workflow);
    }

    /// <summary>
    /// Thêm mới hoặc cập nhật toàn bộ một Workflow.
    /// Tự động xóa cache của compiler để áp dụng luật mới tức thì (Hot-Reload).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Workflow>> SaveWorkflow([FromBody] Workflow workflow)
    {
        if (string.IsNullOrWhiteSpace(workflow.WorkflowId) || string.IsNullOrWhiteSpace(workflow.WorkflowName))
        {
            return BadRequest(new { error = "WorkflowId và WorkflowName là bắt buộc." });
        }

        await _workflowRepo.SaveWorkflowAsync(workflow);

        // Xóa cache để request kế tiếp biên dịch lại rule mới
        _compiler.InvalidateCache();

        return Ok(workflow);
    }

    /// <summary>
    /// Bật hoặc tắt tức thì một Rule cụ thể mà không cần sửa code.
    /// </summary>
    [HttpPut("{workflowId}/rules/{ruleId}/toggle")]
    public async Task<IActionResult> ToggleRule(string workflowId, string ruleId, [FromQuery] bool isActive)
    {
        var success = await _workflowRepo.ToggleRuleStatusAsync(workflowId, ruleId, isActive);
        if (!success)
        {
            return NotFound(new { error = $"Không tìm thấy Rule '{ruleId}' trong Workflow '{workflowId}'." });
        }

        return Ok(new { message = $"Đã {(isActive ? "kích hoạt" : "vô hiệu hóa")} Rule '{ruleId}' thành công." });
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ về cú pháp của biểu thức Rule trước khi lưu.
    /// </summary>
    [HttpPost("validate-rule")]
    public IActionResult ValidateRule([FromBody] ValidateRuleRequest request)
    {
        bool isValid;
        string? error = null;

        if (string.Equals(request.TargetTypeName, nameof(MedicalInsuranceContext), StringComparison.OrdinalIgnoreCase))
        {
            isValid = _compiler.ValidateExpression<MedicalInsuranceContext>(request.Expression, out error);
        }
        else if (string.Equals(request.TargetTypeName, nameof(OrderDiscountContext), StringComparison.OrdinalIgnoreCase))
        {
            isValid = _compiler.ValidateExpression<OrderDiscountContext>(request.Expression, out error);
        }
        else
        {
            return BadRequest(new { error = $"TargetTypeName '{request.TargetTypeName}' không được hỗ trợ để validate." });
        }

        return Ok(new
        {
            expression = request.Expression,
            isValid,
            errorMessage = error
        });
    }
}

public record ValidateRuleRequest(
    string Expression,
    string TargetTypeName
);

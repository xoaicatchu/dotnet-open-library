using Microsoft.AspNetCore.Mvc;
using DynamicRuleEngine.Api.Domain.Interfaces;
using DynamicRuleEngine.Api.Domain.Models;
using DynamicRuleEngine.Api.Domain.UseCases;

namespace DynamicRuleEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EvaluateController : ControllerBase
{
    private readonly IRuleEvaluator _evaluator;

    public EvaluateController(IRuleEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    /// <summary>
    /// Đánh giá mức hưởng Bảo hiểm Y tế (BHYT) của bệnh nhân dựa trên quy tắc động.
    /// Dùng trong hệ thống Quản lý Bệnh viện (HIS) khi tính toán viện phí.
    /// </summary>
    [HttpPost("medical-insurance")]
    [ProducesResponseType<WorkflowExecutionResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkflowExecutionResult>> EvaluateMedicalInsurance(
        [FromBody] MedicalInsuranceContext patientContext,
        CancellationToken ct)
    {
        var result = await _evaluator.EvaluateWorkflowAsync(
            workflowId: "medical-insurance-approval",
            input: patientContext,
            ct: ct);

        return Ok(result);
    }

    /// <summary>
    /// Đánh giá chính sách giảm giá đơn hàng E-Commerce dựa trên quy tắc động.
    /// </summary>
    [HttpPost("order-discount")]
    [ProducesResponseType<WorkflowExecutionResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkflowExecutionResult>> EvaluateOrderDiscount(
        [FromBody] OrderDiscountContext orderContext,
        CancellationToken ct)
    {
        var result = await _evaluator.EvaluateWorkflowAsync(
            workflowId: "order-discount-rules",
            input: orderContext,
            ct: ct);

        return Ok(result);
    }
}

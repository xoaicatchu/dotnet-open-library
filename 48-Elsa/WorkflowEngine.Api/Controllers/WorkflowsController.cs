using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Api.Models;
using WorkflowEngine.Api.Services;

namespace WorkflowEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    private readonly WorkflowService _workflowService;

    public WorkflowsController(WorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    /// <summary>
    /// Lists all available workflow definitions.
    /// </summary>
    [HttpGet("definitions")]
    public ActionResult<List<WorkflowDefinitionDto>> GetDefinitions()
    {
        var definitions = _workflowService.GetAvailableWorkflows();
        return Ok(definitions);
    }

    /// <summary>
    /// Runs the greeting workflow with a name input.
    /// </summary>
    [HttpPost("run/greeting")]
    public async Task<ActionResult<RunWorkflowResponse>> RunGreeting([FromQuery] string name = "World")
    {
        var result = await _workflowService.RunGreetingAsync(name);
        return Ok(result);
    }

    /// <summary>
    /// Runs the order approval workflow.
    /// Orders under $1000 are auto-approved; orders >= $1000 require review.
    /// </summary>
    [HttpPost("run/order-approval")]
    public async Task<ActionResult<RunWorkflowResponse>> RunOrderApproval([FromBody] OrderApprovalInput input)
    {
        if (string.IsNullOrWhiteSpace(input.OrderId))
            return BadRequest(new { Error = "OrderId is required" });

        if (input.Amount <= 0)
            return BadRequest(new { Error = "Amount must be positive" });

        var result = await _workflowService.RunOrderApprovalAsync(input);
        return Ok(result);
    }
}

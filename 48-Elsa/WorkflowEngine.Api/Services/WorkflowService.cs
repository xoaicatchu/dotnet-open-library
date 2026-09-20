using Elsa.Workflows;
using Elsa.Workflows.Options;
using WorkflowEngine.Api.Models;
using WorkflowEngine.Api.Workflows;

namespace WorkflowEngine.Api.Services;

public class WorkflowService
{
    private readonly IWorkflowRunner _workflowRunner;

    public WorkflowService(IWorkflowRunner workflowRunner)
    {
        _workflowRunner = workflowRunner;
    }

    public async Task<RunWorkflowResponse> RunGreetingAsync(string name)
    {
        var workflow = new GreetingWorkflow();

        var options = new RunWorkflowOptions
        {
            Input = new Dictionary<string, object> { ["Name"] = name }
        };

        var result = await _workflowRunner.RunAsync(workflow, options);

        return new RunWorkflowResponse(
            result.WorkflowState.Id,
            result.WorkflowState.Status.ToString());
    }

    public async Task<RunWorkflowResponse> RunOrderApprovalAsync(OrderApprovalInput input)
    {
        var workflow = new OrderApprovalWorkflow();

        var options = new RunWorkflowOptions
        {
            Input = new Dictionary<string, object>
            {
                [OrderApprovalWorkflow.OrderIdInputKey] = input.OrderId,
                [OrderApprovalWorkflow.AmountInputKey] = input.Amount,
                [OrderApprovalWorkflow.RequestedByInputKey] = input.RequestedBy
            }
        };

        var result = await _workflowRunner.RunAsync(workflow, options);

        return new RunWorkflowResponse(
            result.WorkflowState.Id,
            result.WorkflowState.Status.ToString());
    }

    public List<WorkflowDefinitionDto> GetAvailableWorkflows()
    {
        return new List<WorkflowDefinitionDto>
        {
            new("greeting-workflow", "GreetingWorkflow",
                "A simple workflow that greets a user by name",
                1, true, DateTime.UtcNow),
            new("order-approval-workflow", "OrderApprovalWorkflow",
                "Approves or rejects orders based on amount threshold ($1000)",
                1, true, DateTime.UtcNow)
        };
    }
}

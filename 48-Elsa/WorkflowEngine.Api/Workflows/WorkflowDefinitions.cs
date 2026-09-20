using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace WorkflowEngine.Api.Workflows;

/// <summary>
/// A simple workflow that greets a user by name.
/// Demonstrates: Sequential flow, SetVariable, WriteLine.
/// </summary>
public class GreetingWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var nameVariable = builder.WithVariable<string>("Name", "World");
        var messageVariable = builder.WithVariable<string>("Message", "");

        builder.Name = "GreetingWorkflow";
        builder.Description = "A simple workflow that greets a user";
        builder.Version = 1;

        builder.Root = new Sequence
        {
            Activities =
            {
                new SetVariable<string>(messageVariable, context =>
                {
                    var name = nameVariable.Get(context);
                    return $"Hello, {name}! Welcome to Elsa Workflows.";
                }),
                new WriteLine(context => messageVariable.Get(context)!)
            }
        };
    }
}

/// <summary>
/// Order approval workflow: auto-approve orders under threshold, flag large orders.
/// Demonstrates: If/Else branching, variables, sequential activities.
/// </summary>
public class OrderApprovalWorkflow : WorkflowBase
{
    public static readonly string OrderIdInputKey = "OrderId";
    public static readonly string AmountInputKey = "Amount";
    public static readonly string RequestedByInputKey = "RequestedBy";
    public static readonly decimal ApprovalThreshold = 1000m;

    protected override void Build(IWorkflowBuilder builder)
    {
        var orderId = builder.WithVariable<string>(OrderIdInputKey, "");
        var amount = builder.WithVariable<decimal>(AmountInputKey, 0m);
        var requestedBy = builder.WithVariable<string>(RequestedByInputKey, "");
        var isApproved = builder.WithVariable<bool>("IsApproved", false);
        var reason = builder.WithVariable<string>("Reason", "");

        builder.Name = "OrderApprovalWorkflow";
        builder.Description = "Approves or rejects orders based on amount threshold";
        builder.Version = 1;

        builder.Root = new Sequence
        {
            Activities =
            {
                // Step 1: Decision based on amount
                new If(context => amount.Get(context) < ApprovalThreshold)
                {
                    Then = new Sequence
                    {
                        Activities =
                        {
                            new SetVariable<bool>(isApproved, _ => true),
                            new SetVariable<string>(reason, context =>
                                $"Auto-approved: Amount {amount.Get(context):C} is below threshold {ApprovalThreshold:C}"),
                            new WriteLine(context =>
                                $"[APPROVED] Order {orderId.Get(context)} for {amount.Get(context):C} by {requestedBy.Get(context)}")
                        }
                    },
                    Else = new Sequence
                    {
                        Activities =
                        {
                            new SetVariable<bool>(isApproved, _ => false),
                            new SetVariable<string>(reason, context =>
                                $"Requires manual review: Amount {amount.Get(context):C} exceeds threshold {ApprovalThreshold:C}"),
                            new WriteLine(context =>
                                $"[PENDING REVIEW] Order {orderId.Get(context)} for {amount.Get(context):C} by {requestedBy.Get(context)}")
                        }
                    }
                },

                // Step 2: Log result
                new WriteLine(context =>
                    $"Result: Approved={isApproved.Get(context)}, Reason={reason.Get(context)}")
            }
        };
    }
}

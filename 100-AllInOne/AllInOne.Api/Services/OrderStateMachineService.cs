using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Stateless;
using AllInOne.Api.Models;

namespace AllInOne.Api.Services;

public class OrderStateMachineService
{
    public StateMachine<OrderStatus, OrderTrigger> BuildMachine(OrderEntity order)
    {
        var machine = new StateMachine<OrderStatus, OrderTrigger>(
            () => order.Status,
            s => order.Status = s);

        machine.Configure(OrderStatus.Draft)
            .Permit(OrderTrigger.Submit, OrderStatus.Submitted)
            .Permit(OrderTrigger.Cancel, OrderStatus.Cancelled);

        machine.Configure(OrderStatus.Submitted)
            .Permit(OrderTrigger.Approve, OrderStatus.Approved)
            .Permit(OrderTrigger.Cancel, OrderStatus.Cancelled);

        machine.Configure(OrderStatus.Approved)
            .Permit(OrderTrigger.Ship, OrderStatus.Shipped)
            .Permit(OrderTrigger.Cancel, OrderStatus.Cancelled);

        machine.Configure(OrderStatus.Shipped)
            .Permit(OrderTrigger.Complete, OrderStatus.Completed);

        machine.Configure(OrderStatus.Completed).Ignore(OrderTrigger.Complete);
        machine.Configure(OrderStatus.Cancelled);

        return machine;
    }

    public bool CanTransition(OrderEntity order, OrderTrigger trigger)
    {
        var machine = BuildMachine(order);
        return machine.CanFire(trigger);
    }

    public void Transition(OrderEntity order, OrderTrigger trigger)
    {
        var machine = BuildMachine(order);
        machine.Fire(trigger);
    }

    // Elsa (Library 48) Code-First Workflow for Order Approval threshold
    public (bool IsApproved, string Reason) ExecuteApprovalWorkflow(decimal amount)
    {
        bool isApproved = amount < 1000m;
        string reason = isApproved ? "Auto-approved: under $1,000" : "Requires manual review: >= $1,000";
        var workflow = BuildApprovalWorkflow(amount, (a, r) => { });
        return (isApproved, reason);
    }

    public static WorkflowBase BuildApprovalWorkflow(decimal amount, Action<bool, string> onCompleted)
    {
        return new InlineApprovalWorkflow(amount, onCompleted);
    }

    private class InlineApprovalWorkflow : WorkflowBase
    {
        private readonly decimal _amount;
        private readonly Action<bool, string> _onCompleted;

        public InlineApprovalWorkflow(decimal amount, Action<bool, string> onCompleted)
        {
            _amount = amount;
            _onCompleted = onCompleted;
        }

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Name = "OrderApprovalWorkflow";
            builder.Version = 1;

            builder.Root = new Sequence
            {
                Activities =
                {
                    new If(_ => _amount < 1000m)
                    {
                        Then = new Sequence
                        {
                            Activities =
                            {
                                new WriteLine("Order auto-approved: Amount is under $1,000 threshold."),
                                new DelegateActivity(() => _onCompleted(true, "Auto-approved: under $1,000"))
                            }
                        },
                        Else = new Sequence
                        {
                            Activities =
                            {
                                new WriteLine("Order requires manual review: Amount exceeds $1,000 threshold."),
                                new DelegateActivity(() => _onCompleted(false, "Requires manual review: >= $1,000"))
                            }
                        }
                    }
                }
            };
        }
    }

    private class DelegateActivity : CodeActivity
    {
        private readonly Action _action;
        public DelegateActivity(Action action) => _action = action;
        protected override void Execute(ActivityExecutionContext context) => _action();
    }
}

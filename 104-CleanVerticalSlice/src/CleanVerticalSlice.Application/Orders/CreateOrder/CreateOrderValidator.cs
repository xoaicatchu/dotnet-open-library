namespace CleanVerticalSlice.Application.Orders.CreateOrder;

using FluentValidation;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(v => v.Request.CustomerEmail)
            .NotEmpty().WithMessage("CustomerEmail is required.")
            .EmailAddress().WithMessage("A valid email is required.");
            
        RuleFor(v => v.Request.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.");
    }
}

using FluentValidation;
using CustomerValidationFluent.Api.Models;

namespace CustomerValidationFluent.Api.Validators;

public class OrderItemValidator : AbstractValidator<OrderItemDto>
{
    public OrderItemValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .Matches(@"^[A-Z0-9\-]{3,20}$").WithMessage("SKU must contain only uppercase letters, numbers, and hyphens (3-20 chars).");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(150).WithMessage("Product name cannot exceed 150 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
            .LessThanOrEqualTo(1000).WithMessage("Quantity cannot exceed 1000 items per line.");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("UnitPrice must be greater than 0.");
    }
}

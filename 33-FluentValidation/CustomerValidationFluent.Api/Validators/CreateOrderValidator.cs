using FluentValidation;
using CustomerValidationFluent.Api.Models;

namespace CustomerValidationFluent.Api.Validators;

public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    private static readonly string[] AllowedShippingMethods = ["Standard", "Express", "Overnight"];

    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Customer email is required.")
            .EmailAddress().WithMessage("A valid customer email is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.");

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator());

        RuleFor(x => x.ShippingMethod)
            .NotEmpty().WithMessage("Shipping method is required.")
            .Must(m => AllowedShippingMethods.Contains(m))
            .WithMessage($"Shipping method must be one of: {string.Join(", ", AllowedShippingMethods)}.");

        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0, 100).WithMessage("Discount percent must be between 0 and 100.");
    }
}

using FluentValidation;
using CustomerValidationFluent.Api.Models;

namespace CustomerValidationFluent.Api.Validators;

public class CustomerRegistrationValidator : AbstractValidator<CustomerRegistrationRequest>
{
    public CustomerRegistrationValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Length(3, 100).WithMessage("Full name must be between 3 and 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Age)
            .InclusiveBetween(18, 120).WithMessage("Customer must be between 18 and 120 years old.");

        RuleFor(x => x.Address)
            .NotNull().WithMessage("Address is required.")
            .SetValidator(new AddressValidator()!);

        When(x => x.IsVip, () =>
        {
            RuleFor(x => x.MembershipNumber)
                .NotEmpty().WithMessage("Membership number is required for VIP customers.")
                .Matches(@"^VIP-\d{4}$").WithMessage("VIP membership number must follow the format 'VIP-XXXX' (4 digits).");
        });
    }
}

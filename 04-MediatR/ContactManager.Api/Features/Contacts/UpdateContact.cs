using ContactManager.Api.Data;
using FluentValidation;
using MediatR;

namespace ContactManager.Api.Features.Contacts;

public record UpdateContactCommand(int Id, string FirstName, string LastName, string Email, string? Phone, string? Company) 
    : IRequest<bool>;

public class UpdateContactValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Company).MaximumLength(200);
    }
}

public class UpdateContactHandler : IRequestHandler<UpdateContactCommand, bool>
{
    private readonly ContactStore _store;

    public UpdateContactHandler(ContactStore store)
    {
        _store = store;
    }

    public Task<bool> Handle(UpdateContactCommand request, CancellationToken cancellationToken)
    {
        var existing = _store.GetById(request.Id);
        if (existing == null) return Task.FromResult(false);

        existing.FirstName = request.FirstName;
        existing.LastName = request.LastName;
        existing.Email = request.Email;
        existing.Phone = request.Phone;
        existing.Company = request.Company;

        var result = _store.Update(existing);
        return Task.FromResult(result);
    }
}

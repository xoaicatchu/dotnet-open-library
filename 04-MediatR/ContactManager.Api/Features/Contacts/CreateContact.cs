using ContactManager.Api.Data;
using ContactManager.Api.Models;
using FluentValidation;
using MediatR;

namespace ContactManager.Api.Features.Contacts;

public record ContactDto(int Id, string FirstName, string LastName, string Email, string? Phone, string? Company);

public record CreateContactCommand(string FirstName, string LastName, string Email, string? Phone, string? Company) 
    : IRequest<ContactDto>;

public class CreateContactValidator : AbstractValidator<CreateContactCommand>
{
    public CreateContactValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Company).MaximumLength(200);
    }
}

public class CreateContactHandler : IRequestHandler<CreateContactCommand, ContactDto>
{
    private readonly ContactStore _store;

    public CreateContactHandler(ContactStore store)
    {
        _store = store;
    }

    public Task<ContactDto> Handle(CreateContactCommand request, CancellationToken cancellationToken)
    {
        var contact = new Contact
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Company = request.Company
        };

        var created = _store.Add(contact);
        return Task.FromResult(new ContactDto(created.Id, created.FirstName, created.LastName, created.Email, created.Phone, created.Company));
    }
}

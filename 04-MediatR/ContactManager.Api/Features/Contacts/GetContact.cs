using ContactManager.Api.Data;
using MediatR;

namespace ContactManager.Api.Features.Contacts;

public record GetContactQuery(int Id) : IRequest<ContactDto?>;

public class GetContactHandler : IRequestHandler<GetContactQuery, ContactDto?>
{
    private readonly ContactStore _store;

    public GetContactHandler(ContactStore store)
    {
        _store = store;
    }

    public Task<ContactDto?> Handle(GetContactQuery request, CancellationToken cancellationToken)
    {
        var contact = _store.GetById(request.Id);
        if (contact == null) return Task.FromResult<ContactDto?>(null);

        return Task.FromResult<ContactDto?>(new ContactDto(
            contact.Id,
            contact.FirstName,
            contact.LastName,
            contact.Email,
            contact.Phone,
            contact.Company));
    }
}

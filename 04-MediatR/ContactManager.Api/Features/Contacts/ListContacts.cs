using ContactManager.Api.Data;
using MediatR;

namespace ContactManager.Api.Features.Contacts;

public record ListContactsQuery(string? Search = null) : IRequest<IEnumerable<ContactDto>>;

public class ListContactsHandler : IRequestHandler<ListContactsQuery, IEnumerable<ContactDto>>
{
    private readonly ContactStore _store;

    public ListContactsHandler(ContactStore store)
    {
        _store = store;
    }

    public Task<IEnumerable<ContactDto>> Handle(ListContactsQuery request, CancellationToken cancellationToken)
    {
        var contacts = _store.GetAll();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLowerInvariant();
            contacts = contacts.Where(c => 
                (c.FirstName != null && c.FirstName.ToLowerInvariant().Contains(search)) ||
                (c.LastName != null && c.LastName.ToLowerInvariant().Contains(search)) ||
                (c.Email != null && c.Email.ToLowerInvariant().Contains(search))
            );
        }

        var dtos = contacts.Select(c => new ContactDto(
            c.Id,
            c.FirstName,
            c.LastName,
            c.Email,
            c.Phone,
            c.Company
        ));

        return Task.FromResult(dtos);
    }
}

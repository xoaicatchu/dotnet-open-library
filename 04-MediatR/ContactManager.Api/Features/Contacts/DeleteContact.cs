using ContactManager.Api.Data;
using MediatR;

namespace ContactManager.Api.Features.Contacts;

public record DeleteContactCommand(int Id) : IRequest<bool>;

public class DeleteContactHandler : IRequestHandler<DeleteContactCommand, bool>
{
    private readonly ContactStore _store;

    public DeleteContactHandler(ContactStore store)
    {
        _store = store;
    }

    public Task<bool> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
    {
        var result = _store.Delete(request.Id);
        return Task.FromResult(result);
    }
}

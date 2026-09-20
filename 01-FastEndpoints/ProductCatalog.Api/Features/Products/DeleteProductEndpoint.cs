using FastEndpoints;
using ProductCatalog.Api.Data;

namespace ProductCatalog.Api.Features.Products;

public class DeleteProductEndpoint : Endpoint<DeleteProductRequest>
{
    private readonly ProductStore _store;

    public DeleteProductEndpoint(ProductStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Delete("/api/products/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DeleteProductRequest req, CancellationToken ct)
    {
        var product = _store.GetById(req.Id);
        if (product == null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        _store.Delete(req.Id);
        await HttpContext.Response.SendNoContentAsync(ct);
    }
}

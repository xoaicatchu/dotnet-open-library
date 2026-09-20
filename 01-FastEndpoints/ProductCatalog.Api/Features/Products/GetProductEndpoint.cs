using FastEndpoints;
using ProductCatalog.Api.Data;

namespace ProductCatalog.Api.Features.Products;

public class GetProductEndpoint : Endpoint<GetProductRequest, ProductResponse>
{
    private readonly ProductStore _store;

    public GetProductEndpoint(ProductStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/api/products/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetProductRequest req, CancellationToken ct)
    {
        var product = _store.GetById(req.Id);
        if (product == null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        var response = new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock
        };

        await HttpContext.Response.SendAsync(response, cancellation: ct);
    }
}

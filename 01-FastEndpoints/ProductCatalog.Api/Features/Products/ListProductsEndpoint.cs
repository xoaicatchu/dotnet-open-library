using FastEndpoints;
using ProductCatalog.Api.Data;

namespace ProductCatalog.Api.Features.Products;

public class ListProductsEndpoint : Endpoint<SearchProductsRequest, IEnumerable<ProductResponse>>
{
    private readonly ProductStore _store;

    public ListProductsEndpoint(ProductStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/api/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SearchProductsRequest req, CancellationToken ct)
    {
        var products = _store.GetAll();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            products = products.Where(p => p.Name.Contains(req.Search, StringComparison.OrdinalIgnoreCase));
        }

        var response = products.Select(p => new ProductResponse
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            Stock = p.Stock
        });

        await HttpContext.Response.SendAsync(response, cancellation: ct);
    }
}

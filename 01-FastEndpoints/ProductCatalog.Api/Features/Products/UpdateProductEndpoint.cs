using FastEndpoints;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

namespace ProductCatalog.Api.Features.Products;

public class UpdateProductEndpoint : Endpoint<UpdateProductRequest, ProductResponse>
{
    private readonly ProductStore _store;

    public UpdateProductEndpoint(ProductStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Put("/api/products/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateProductRequest req, CancellationToken ct)
    {
        var product = _store.GetById(req.Id);
        if (product == null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        product.Name = req.Name.Trim();
        product.Price = req.Price;
        product.Stock = req.Stock;

        _store.Update(product);

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

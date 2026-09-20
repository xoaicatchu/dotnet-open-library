using FastEndpoints;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

namespace ProductCatalog.Api.Features.Products;

public class CreateProductEndpoint : Endpoint<CreateProductRequest, ProductResponse>
{
    private readonly ProductStore _store;

    public CreateProductEndpoint(ProductStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Post("/api/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
    {
        var product = new Product
        {
            Name = req.Name.Trim(),
            Price = req.Price,
            Stock = req.Stock
        };

        var created = _store.Create(product);

        var response = new ProductResponse
        {
            Id = created.Id,
            Name = created.Name,
            Price = created.Price,
            Stock = created.Stock
        };

        await HttpContext.Response.SendCreatedAtAsync<GetProductEndpoint>(
            new { id = created.Id }, 
            response, 
            generateAbsoluteUrl: true, 
            cancellation: ct);
    }
}

namespace CleanVerticalSlice.Application.Products.CreateProduct;

using CleanVerticalSlice.Application.Products.GetProducts;
using MediatR;

public record CreateProductCommand(CreateProductRequest Request) : IRequest<ProductDto>;

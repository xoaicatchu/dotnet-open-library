namespace CleanVerticalSlice.Application.Products.UpdateProduct;

using CleanVerticalSlice.Application.Products.GetProducts;
using MediatR;

public record UpdateProductCommand(int Id, UpdateProductRequest Request) : IRequest<ProductDto?>;

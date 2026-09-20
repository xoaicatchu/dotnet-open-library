namespace CleanVerticalSlice.Application.Products.GetProductById;

using MediatR;

public record GetProductByIdQuery(int Id) : IRequest<ProductDetailDto?>;

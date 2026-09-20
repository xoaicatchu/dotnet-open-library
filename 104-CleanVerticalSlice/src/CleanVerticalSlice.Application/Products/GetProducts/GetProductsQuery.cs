namespace CleanVerticalSlice.Application.Products.GetProducts;

using System.Collections.Generic;
using MediatR;

public record GetProductsQuery : IRequest<List<ProductDto>>;

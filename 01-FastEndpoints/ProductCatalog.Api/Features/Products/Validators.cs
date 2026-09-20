using FastEndpoints;
using FluentValidation;

namespace ProductCatalog.Api.Features.Products;

public class CreateProductValidator : Validator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("name is required")
            .MaximumLength(100).WithMessage("name cannot exceed 100 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("price must be greater than 0");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("stock must be zero or positive");
    }
}

public class UpdateProductValidator : Validator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("id must be greater than 0");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("name is required")
            .MaximumLength(100).WithMessage("name cannot exceed 100 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("price must be greater than 0");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("stock must be zero or positive");
    }
}

public class GetProductValidator : Validator<GetProductRequest>
{
    public GetProductValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("id must be greater than 0");
    }
}

public class DeleteProductValidator : Validator<DeleteProductRequest>
{
    public DeleteProductValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("id must be greater than 0");
    }
}

using AutoMapper;
using EnterpriseClassic.Api.Data;
using FluentValidation;
using MediatR;

namespace EnterpriseClassic.Api.Features.Products;

public record CreateProductCommand(CreateProductRequest Request) : IRequest<ProductDto>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateProductRequest> _validator;

    public CreateProductCommandHandler(AppDbContext db, IMapper mapper, IValidator<CreateProductRequest> validator)
    {
        _db = db;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request.Request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var entity = _mapper.Map<ProductEntity>(request.Request);
        entity.CreatedAt = DateTime.UtcNow;

        _db.Products.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductDto>(entity);
    }
}

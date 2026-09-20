using System;
using System.Threading.Tasks;
using EventSourcing.Api.Domain.Products;
using EventSourcing.Api.Domain.Products.Events;
using EventSourcing.Api.EventStore;
using EventSourcing.Api.ReadModel;
namespace EventSourcing.Api.Handlers.Commands;

public record CreateProductCommand(string Name, decimal Price, int Stock);
public record UpdateProductCommand(Guid Id, string Name, decimal Price, int Stock);
public record DeactivateProductCommand(Guid Id);

public class CreateProductHandler
{
    private readonly IEventStore _eventStore;
    private readonly IProductReadRepository _readRepo;
    
    public CreateProductHandler(IEventStore eventStore, IProductReadRepository readRepo)
    {
        _eventStore = eventStore; _readRepo = readRepo;
    }

    public async Task<Guid> Handle(CreateProductCommand cmd)
    {
        var product = Product.Create(cmd.Name, cmd.Price, cmd.Stock);
        await _eventStore.AppendEventsAsync(product.Id, nameof(Product), product.UncommittedEvents, 0);
        foreach (var e in product.UncommittedEvents)
            if (e is ProductCreated created) _readRepo.Apply(created);
        product.ClearUncommittedEvents();
        return product.Id;
    }
}

public class UpdateProductHandler
{
    private readonly IEventStore _eventStore;
    private readonly IProductReadRepository _readRepo;
    
    public UpdateProductHandler(IEventStore eventStore, IProductReadRepository readRepo)
    {
        _eventStore = eventStore; _readRepo = readRepo;
    }

    public async Task Handle(UpdateProductCommand cmd)
    {
        var product = await _eventStore.ReconstructAggregateAsync<Product>(cmd.Id);
        if (product == null) throw new InvalidOperationException("Product not found");
        
        var expectedVersion = product.Version;
        product.Update(cmd.Name, cmd.Price, cmd.Stock);
        
        await _eventStore.AppendEventsAsync(product.Id, nameof(Product), product.UncommittedEvents, expectedVersion);
        foreach (var e in product.UncommittedEvents)
            if (e is ProductUpdated updated) _readRepo.Apply(updated);
        product.ClearUncommittedEvents();
    }
}

public class DeactivateProductHandler
{
    private readonly IEventStore _eventStore;
    private readonly IProductReadRepository _readRepo;
    
    public DeactivateProductHandler(IEventStore eventStore, IProductReadRepository readRepo)
    {
        _eventStore = eventStore; _readRepo = readRepo;
    }

    public async Task Handle(DeactivateProductCommand cmd)
    {
        var product = await _eventStore.ReconstructAggregateAsync<Product>(cmd.Id);
        if (product == null) throw new InvalidOperationException("Product not found");
        
        var expectedVersion = product.Version;
        product.Deactivate();
        
        await _eventStore.AppendEventsAsync(product.Id, nameof(Product), product.UncommittedEvents, expectedVersion);
        foreach (var e in product.UncommittedEvents)
            if (e is ProductDeactivated deactivated) _readRepo.Apply(deactivated);
        product.ClearUncommittedEvents();
    }
}

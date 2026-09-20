using Rebus.Handlers;
using Rebus.Bus;
using InventoryService.Api.Messages;
using InventoryService.Api.Data;

namespace InventoryService.Api.Handlers;

public class AddStockHandler : IHandleMessages<AddStockCommand>
{
    private readonly InventoryStore _store;
    private readonly IBus _bus;
    
    public AddStockHandler(InventoryStore store, IBus bus)
    {
        _store = store;
        _bus = bus;
    }
    
    public async Task Handle(AddStockCommand message)
    {
        var item = _store.AddStock(message.ItemId, message.Amount);
        if (item != null)
        {
            await _bus.Publish(new StockUpdatedEvent(item.Id, item.Sku, item.Quantity, "Added"));
        }
    }
}

public class RemoveStockHandler : IHandleMessages<RemoveStockCommand>
{
    private readonly InventoryStore _store;
    private readonly IBus _bus;
    
    public RemoveStockHandler(InventoryStore store, IBus bus)
    {
        _store = store;
        _bus = bus;
    }
    
    public async Task Handle(RemoveStockCommand message)
    {
        var item = _store.RemoveStock(message.ItemId, message.Amount);
        if (item != null)
        {
            await _bus.Publish(new StockUpdatedEvent(item.Id, item.Sku, item.Quantity, "Removed"));
        }
    }
}

public class StockUpdatedHandler : IHandleMessages<StockUpdatedEvent>
{
    private readonly ILogger<StockUpdatedHandler> _logger;

    public StockUpdatedHandler(ILogger<StockUpdatedHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(StockUpdatedEvent message)
    {
        _logger.LogInformation("Stock Updated: Item {ItemId} ({Sku}) {ChangeType}. New Quantity: {NewQuantity}", 
            message.ItemId, message.Sku, message.ChangeType, message.NewQuantity);
        return Task.CompletedTask;
    }
}

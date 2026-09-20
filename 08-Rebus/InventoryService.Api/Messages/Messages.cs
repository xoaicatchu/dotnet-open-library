namespace InventoryService.Api.Messages;

public record AddStockCommand(int ItemId, int Amount);
public record RemoveStockCommand(int ItemId, int Amount);
public record StockUpdatedEvent(int ItemId, string Sku, int NewQuantity, string ChangeType);

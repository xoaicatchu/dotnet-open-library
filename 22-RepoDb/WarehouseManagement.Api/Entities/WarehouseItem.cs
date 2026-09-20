using RepoDb.Attributes;

namespace WarehouseManagement.Api.Entities;

[Map("WarehouseItems")]
public class WarehouseItem
{
    [Identity]
    [Primary]
    public long Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    [PropertyHandler(typeof(Data.SqliteDateTimeHandler))]
    public DateTime LastRestockedAt { get; set; } = DateTime.UtcNow;
}

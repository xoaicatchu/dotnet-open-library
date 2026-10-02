namespace StockFlow.Api.Auth;
public static class RolePolicies
{
    public const string Admin = "Admin";
    public const string Warehouse = "Warehouse";
    public const string Viewer = "Viewer";
    public const string WriteInventory = "WriteInventory";
    public static readonly string[] Roles = [Admin, Warehouse, Viewer];
}

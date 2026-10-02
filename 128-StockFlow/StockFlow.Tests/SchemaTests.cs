using Npgsql;
using StockFlow.Tests.Infrastructure;

namespace StockFlow.Tests;

[Collection("Postgres")]
public sealed class SchemaTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Inventory_schema_migration_has_required_constraints()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        _ = factory.CreateClient();
        await using var connection = new NpgsqlConnection(postgres.Database.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM information_schema.tables WHERE table_name IN ('Products','StockBalances','StockDocuments','StockMovements','ClientOperations','DataProtectionKeys')", connection);
        Assert.Equal(6L, await command.ExecuteScalarAsync());
        command.CommandText = "SELECT count(*) FROM pg_constraint WHERE conname IN ('CK_Balance_Numbers','CK_Movement_Delta','CK_Document_Version','CK_Line_Numbers')";
        Assert.Equal(4L, await command.ExecuteScalarAsync());
        command.CommandText = "SELECT count(*) FROM information_schema.columns WHERE table_name='StockDocumentLines' AND column_name='ObservedBalanceVersion'";
        Assert.Equal(1L, await command.ExecuteScalarAsync());
        command.CommandText = "INSERT INTO \"Products\" (\"Id\",\"Sku\",\"Name\",\"Unit\",\"PurchasePriceVnd\",\"SalePriceVnd\",\"MinimumStock\",\"IsActive\") VALUES ('00000000-0000-0000-0000-000000000001','NEGATIVE','Invalid','cai',-1,0,0,true)";
        var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("23514", error.SqlState);
    }
}

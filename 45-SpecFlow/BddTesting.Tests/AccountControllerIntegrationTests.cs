using System.Net;
using System.Net.Http.Json;
using BddTesting.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BddTesting.Tests;

public class AccountControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AccountControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllAccounts_Returns200WithPreseededAccounts()
    {
        // Act
        var response = await _client.GetAsync("/api/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var accounts = await response.Content.ReadFromJsonAsync<List<BankAccount>>();
        Assert.NotNull(accounts);
        Assert.True(accounts.Count >= 3);
        Assert.Contains(accounts, a => a.AccountNumber == "ACC-1001");
    }

    [Fact]
    public async Task GetAccountByNumber_Valid_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/api/accounts/ACC-1001");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var account = await response.Content.ReadFromJsonAsync<BankAccount>();
        Assert.NotNull(account);
        Assert.Equal("Alice Smith", account.AccountHolder);
    }

    [Fact]
    public async Task GetAccountByNumber_NotFound_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/accounts/NON_EXISTENT_ACC");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_Valid_Returns201WithLocation()
    {
        // Arrange
        var request = new CreateAccountRequest("ACC-9001", "Emma Watson", 1200.00m, "USD");

        // Act
        var response = await _client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<BankAccount>();
        Assert.NotNull(created);
        Assert.Equal("ACC-9001", created.AccountNumber);
        Assert.Equal(1200.00m, created.Balance);
    }

    [Fact]
    public async Task Transfer_Valid_Returns200WithUpdatedBalances()
    {
        // Arrange: Create 2 fresh accounts to avoid state pollution
        var fromAcc = "ACC-TRANS-1";
        var toAcc = "ACC-TRANS-2";
        await _client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest(fromAcc, "Sender", 500m));
        await _client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest(toAcc, "Receiver", 100m));

        var request = new TransferRequest(fromAcc, toAcc, 200m, "Test transfer");

        // Act
        var response = await _client.PostAsJsonAsync("/api/accounts/transfer", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TransferResult>();
        Assert.NotNull(result);
        Assert.Equal(300m, result.FromRemainingBalance);
        Assert.Equal(300m, result.ToNewBalance);
        Assert.Equal("Completed", result.Status);
    }

    [Fact]
    public async Task Transfer_InsufficientFunds_Returns400()
    {
        // Arrange
        var fromAcc = "ACC-POOR-1";
        var toAcc = "ACC-RICH-1";
        await _client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest(fromAcc, "Poor Sender", 20m));
        await _client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest(toAcc, "Rich Receiver", 500m));

        var request = new TransferRequest(fromAcc, toAcc, 100m, "Failed transfer");

        // Act
        var response = await _client.PostAsJsonAsync("/api/accounts/transfer", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_InactiveAccount_Returns400()
    {
        // Arrange: ACC-1003 is seeded as inactive
        var request = new TransferRequest("ACC-1001", "ACC-1003", 50m, "Transfer to inactive");

        // Act
        var response = await _client.PostAsJsonAsync("/api/accounts/transfer", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

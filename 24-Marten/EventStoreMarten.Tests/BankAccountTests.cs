using System.Net;
using System.Net.Http.Json;
using EventStoreMarten.Api.Domain;
using EventStoreMarten.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventStoreMarten.Tests;

public class BankAccountAggregateTests
{
    [Fact]
    public void Apply_AccountCreated_SetsInitialState()
    {
        var account = new BankAccount();
        var accountId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        account.Apply(new AccountCreated(accountId, "John Doe", 1000m, now));

        Assert.Equal(accountId, account.Id);
        Assert.Equal("John Doe", account.OwnerName);
        Assert.Equal(1000m, account.Balance);
        Assert.Equal(now, account.CreatedAt);
    }

    [Fact]
    public void Apply_MoneyDeposited_IncreasesBalance()
    {
        var account = new BankAccount();
        var accountId = Guid.NewGuid();
        account.Apply(new AccountCreated(accountId, "John Doe", 500m, DateTime.UtcNow));

        account.Apply(new MoneyDeposited(accountId, 200m, "Deposit", DateTime.UtcNow));

        Assert.Equal(700m, account.Balance);
    }

    [Fact]
    public void Apply_MoneyWithdrawn_DecreasesBalance()
    {
        var account = new BankAccount();
        var accountId = Guid.NewGuid();
        account.Apply(new AccountCreated(accountId, "John Doe", 500m, DateTime.UtcNow));

        account.Apply(new MoneyWithdrawn(accountId, 150m, "Withdrawal", DateTime.UtcNow));

        Assert.Equal(350m, account.Balance);
    }
}

public class BankAccountApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BankAccountApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAccount_WithValidData_ReturnsCreated()
    {
        var request = new CreateAccountRequest("Alice Johnson", 300m);
        var response = await _client.PostAsJsonAsync("/api/bankaccounts", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var account = await response.Content.ReadFromJsonAsync<AccountResponseDto>();
        Assert.NotNull(account);
        Assert.Equal("Alice Johnson", account.OwnerName);
        Assert.Equal(300m, account.Balance);
    }

    [Fact]
    public async Task CreateAccount_WithNegativeBalance_ReturnsBadRequest()
    {
        var request = new CreateAccountRequest("Bob Invalid", -50m);
        var response = await _client.PostAsJsonAsync("/api/bankaccounts", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAccount_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/bankaccounts/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Deposit_IncreasesBalanceAndAppendsEvent()
    {
        // 1. Create account
        var createRes = await _client.PostAsJsonAsync("/api/bankaccounts", new CreateAccountRequest("Charlie", 100m));
        var created = await createRes.Content.ReadFromJsonAsync<AccountResponseDto>();
        Assert.NotNull(created);

        // 2. Deposit
        var depositRes = await _client.PostAsJsonAsync($"/api/bankaccounts/{created.Id}/deposit", new DepositRequest(150m, "Bonus"));
        Assert.Equal(HttpStatusCode.OK, depositRes.StatusCode);

        var updated = await depositRes.Content.ReadFromJsonAsync<AccountResponseDto>();
        Assert.NotNull(updated);
        Assert.Equal(250m, updated.Balance);

        // 3. Check events stream
        var eventsRes = await _client.GetAsync($"/api/bankaccounts/{created.Id}/events");
        Assert.Equal(HttpStatusCode.OK, eventsRes.StatusCode);
        var events = await eventsRes.Content.ReadFromJsonAsync<List<EventStreamItemDto>>();
        Assert.NotNull(events);
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.EventType == nameof(MoneyDeposited));
    }

    [Fact]
    public async Task Withdraw_DecreasesBalance_AndRejectsOverdraft()
    {
        // 1. Create account
        var createRes = await _client.PostAsJsonAsync("/api/bankaccounts", new CreateAccountRequest("Diana", 200m));
        var created = await createRes.Content.ReadFromJsonAsync<AccountResponseDto>();
        Assert.NotNull(created);

        // 2. Withdraw within balance
        var withdrawRes = await _client.PostAsJsonAsync($"/api/bankaccounts/{created.Id}/withdraw", new WithdrawRequest(50m, "Groceries"));
        Assert.Equal(HttpStatusCode.OK, withdrawRes.StatusCode);
        var updated = await withdrawRes.Content.ReadFromJsonAsync<AccountResponseDto>();
        Assert.NotNull(updated);
        Assert.Equal(150m, updated.Balance);

        // 3. Overdraft attempt should return BadRequest
        var overdraftRes = await _client.PostAsJsonAsync($"/api/bankaccounts/{created.Id}/withdraw", new WithdrawRequest(500m, "Too much"));
        Assert.Equal(HttpStatusCode.BadRequest, overdraftRes.StatusCode);
    }

    [Fact]
    public async Task CustomerDocument_SaveAndLoad_WorksCorrectly()
    {
        var request = new CreateCustomerRequest("Eve Adams", "eve@example.com", "123-456-7890", "Gold");
        var createRes = await _client.PostAsJsonAsync("/api/bankaccounts/customers", request);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var customer = await createRes.Content.ReadFromJsonAsync<CustomerResponseDto>();
        Assert.NotNull(customer);
        Assert.Equal("Eve Adams", customer.FullName);
        Assert.Equal("Gold", customer.Tier);

        // Load document by ID
        var getRes = await _client.GetAsync($"/api/bankaccounts/customers/{customer.Id}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var loaded = await getRes.Content.ReadFromJsonAsync<CustomerResponseDto>();
        Assert.NotNull(loaded);
        Assert.Equal(customer.Id, loaded.Id);
    }

    [Fact]
    public async Task GetCustomer_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/bankaccounts/customers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

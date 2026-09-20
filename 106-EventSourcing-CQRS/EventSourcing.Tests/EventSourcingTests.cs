using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EventSourcing.Api.Domain.Common;
using EventSourcing.Api.Domain.Products;
using EventSourcing.Api.Domain.Products.Events;
using EventSourcing.Api.EventStore;
using EventSourcing.Api.Handlers.Commands;
using EventSourcing.Api.ReadModel;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventSourcing.Tests;

public class EventSourcingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public EventSourcingTests(WebApplicationFactory<Program> fixture)
    {
        var dbName = "test_" + Guid.NewGuid().ToString() + ".db";
        _factory = fixture.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d => d.ServiceType.Name.Contains("DbContextOptions")).ToList();
                foreach(var d in descriptors) services.Remove(d);

                services.AddDbContext<EventStoreDbContext>(options =>
                {
                    options.UseSqlite("Data Source=" + dbName);
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_Returns201AndStoresEvent()
    {
        var cmd = new CreateProductCommand("Test Product", 10.5m, 100);
        var response = await _client.PostAsJsonAsync("/api/products", cmd);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var id = Guid.Parse((await response.Content.ReadAsStringAsync()).Trim('"'));
        
        using var scope = _factory.Services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var events = await eventStore.GetEventsAsync(id);
        events.Should().ContainSingle();
        events.First().Should().BeOfType<ProductCreated>();
    }

    [Fact]
    public async Task GetProducts_Returns200WithList()
    {
        var cmd = new CreateProductCommand("P1", 10, 10);
        var res = await _client.PostAsJsonAsync("/api/products", cmd);
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var response = await _client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ProductReadModel>>();
        products.Should().NotBeNull();
        products!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetProductById_Returns200ReadModel()
    {
        var res = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand("P2", 20, 20));
        var id = Guid.Parse((await res.Content.ReadAsStringAsync()).Trim('"'));
        
        var response = await _client.GetAsync($"/api/products/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductReadModel>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("P2");
    }

    [Fact]
    public async Task GetProductById_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_Returns200AndStoresEvent()
    {
        var createRes = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand("P3", 30, 30));
        var id = Guid.Parse((await createRes.Content.ReadAsStringAsync()).Trim('"'));

        var updateCmd = new UpdateProductCommand(id, "P3 Updated", 35, 25);
        var updateRes = await _client.PutAsJsonAsync($"/api/products/{id}", updateCmd);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var eventsRes = await _client.GetAsync($"/api/events/{id}");
        var events = await eventsRes.Content.ReadFromJsonAsync<List<object>>();
        events.Should().HaveCount(2); 
    }

    [Fact]
    public async Task UpdateProduct_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        var updateCmd = new UpdateProductCommand(id, "P3 Updated", 35, 25);
        var updateRes = await _client.PutAsJsonAsync($"/api/products/{id}", updateCmd);
        updateRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateProduct_Returns204()
    {
        var createRes = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand("P4", 40, 40));
        var id = Guid.Parse((await createRes.Content.ReadAsStringAsync()).Trim('"'));

        var delRes = await _client.DeleteAsync($"/api/products/{id}");
        delRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getRes = await _client.GetAsync($"/api/products/{id}");
        var product = await getRes.Content.ReadFromJsonAsync<ProductReadModel>();
        product!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetEvents_Returns200WithHistory()
    {
        var createRes = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand("P5", 50, 50));
        var id = Guid.Parse((await createRes.Content.ReadAsStringAsync()).Trim('"'));

        var getRes = await _client.GetAsync($"/api/events/{id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EventReplay_VerifyOrder()
    {
        var res = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand("P6", 60, 60));
        var id = Guid.Parse((await res.Content.ReadAsStringAsync()).Trim('"'));

        await _client.PutAsJsonAsync($"/api/products/{id}", new UpdateProductCommand(id, "P6.1", 61, 61));
        await _client.PutAsJsonAsync($"/api/products/{id}", new UpdateProductCommand(id, "P6.2", 62, 62));
        await _client.PutAsJsonAsync($"/api/products/{id}", new UpdateProductCommand(id, "P6.3", 63, 63));

        using var scope = _factory.Services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var events = await eventStore.GetEventsAsync(id);
        events.Should().HaveCount(4);
        events[0].Should().BeOfType<ProductCreated>();
        events[1].Should().BeOfType<ProductUpdated>();
        events[2].Should().BeOfType<ProductUpdated>();
        events[3].Should().BeOfType<ProductUpdated>();
    }

    [Fact]
    public async Task UpdateDeactivatedProduct_Returns400()
    {
        var res = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand("P7", 70, 70));
        var id = Guid.Parse((await res.Content.ReadAsStringAsync()).Trim('"'));

        await _client.DeleteAsync($"/api/products/{id}");
        var putRes = await _client.PutAsJsonAsync($"/api/products/{id}", new UpdateProductCommand(id, "P7.1", 71, 71));
        putRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReadModelReflectsLatestState()
    {
        var res = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand("P8", 80, 80));
        var id = Guid.Parse((await res.Content.ReadAsStringAsync()).Trim('"'));

        await _client.PutAsJsonAsync($"/api/products/{id}", new UpdateProductCommand(id, "P8.Final", 99, 99));
        
        var getRes = await _client.GetAsync($"/api/products/{id}");
        var product = await getRes.Content.ReadFromJsonAsync<ProductReadModel>();
        product!.Name.Should().Be("P8.Final");
        product.Price.Should().Be(99);
    }

    [Fact]
    public async Task AggregateReconstruction_VerifyVersionIncrements()
    {
        var id = Guid.NewGuid();
        var events = new List<IEvent>
        {
            new ProductCreated(id, "P9", 90, 90, DateTime.UtcNow, 1),
            new ProductUpdated(id, "P9.1", 91, 91, DateTime.UtcNow, 2)
        };
        
        var aggregate = new Product();
        aggregate.LoadFromHistory(events);
        
        aggregate.Version.Should().Be(2);
        aggregate.Name.Should().Be("P9.1");
    }
}

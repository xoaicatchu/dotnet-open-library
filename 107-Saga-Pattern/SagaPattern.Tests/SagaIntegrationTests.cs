using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SagaPattern.Api.Consumers;
using SagaPattern.Api.Messages;
using SagaPattern.Api.Models;
using SagaPattern.Api.Sagas;
using Xunit;

namespace SagaPattern.Tests;

public class SagaIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SagaIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Test1_HappyPath_CompletesSaga()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddSagaStateMachine<OrderSaga, OrderSagaState>().InMemoryRepository();
                x.AddConsumer<PaymentConsumer>();
                x.AddConsumer<InventoryConsumer>();
                x.AddConsumer<ShippingConsumer>();
            })
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var orderId = Guid.NewGuid();
        await harness.Bus.Publish(new OrderPlaced(orderId, "Customer", 100));
        await Task.Delay(2000);
        var sagaHarness = harness.GetSagaStateMachineHarness<OrderSaga, OrderSagaState>();
        var saga = sagaHarness.Sagas.Contains(orderId);
        saga.Should().NotBeNull();
        saga!.CurrentState.Should().Be("Completed");
    }

    [Fact]
    public async Task Test2_PaymentFails_AmountExceedsLimit()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddSagaStateMachine<OrderSaga, OrderSagaState>().InMemoryRepository();
                x.AddConsumer<PaymentConsumer>();
            })
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var orderId = Guid.NewGuid();
        await harness.Bus.Publish(new OrderPlaced(orderId, "Customer", 1000));
        await Task.Delay(2000);
        var sagaHarness = harness.GetSagaStateMachineHarness<OrderSaga, OrderSagaState>();
        var saga = sagaHarness.Sagas.Contains(orderId);
        saga.Should().NotBeNull();
        saga!.CurrentState.Should().Be("Failed");
        saga.FailureReason.Should().Be("Amount exceeds limit");
    }

    [Fact]
    public async Task Test3_SagaState_PersistsCorrelationId()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddSagaStateMachine<OrderSaga, OrderSagaState>().InMemoryRepository();
                x.AddConsumer<PaymentConsumer>();
            })
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var orderId = Guid.NewGuid();
        await harness.Bus.Publish(new OrderPlaced(orderId, "Customer", 100));
        await Task.Delay(1000);
        var sagaHarness = harness.GetSagaStateMachineHarness<OrderSaga, OrderSagaState>();
        var saga = sagaHarness.Sagas.Contains(orderId);
        saga.Should().NotBeNull();
        saga!.CorrelationId.Should().Be(orderId);
    }

    [Fact]
    public async Task Test4_PostOrder_ReturnsAccepted()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest { CustomerName = "Test", TotalAmount = 100 });
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Test5_PostOrder_InvalidRequest_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest { CustomerName = "", TotalAmount = 100 });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Test6_VerifyPaymentConsumer_IsConsumedAfterOrderPlaced()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddSagaStateMachine<OrderSaga, OrderSagaState>().InMemoryRepository();
                x.AddConsumer<PaymentConsumer>();
            })
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        await harness.Bus.Publish(new OrderPlaced(Guid.NewGuid(), "Customer", 100));
        await Task.Delay(1000);
        Assert.True(await harness.GetConsumerHarness<PaymentConsumer>().Consumed.Any<ProcessPaymentCommand>());
    }

    [Fact]
    public async Task Test7_VerifyInventoryConsumer_IsConsumedAfterPaymentProcessed()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddSagaStateMachine<OrderSaga, OrderSagaState>().InMemoryRepository();
                x.AddConsumer<PaymentConsumer>();
                x.AddConsumer<InventoryConsumer>();
            })
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        await harness.Bus.Publish(new OrderPlaced(Guid.NewGuid(), "Customer", 100));
        await Task.Delay(2000);
        Assert.True(await harness.GetConsumerHarness<InventoryConsumer>().Consumed.Any<ReserveInventoryCommand>());
    }

    [Fact]
    public async Task Test8_VerifyShippingConsumer_IsConsumedAfterInventoryReserved()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddSagaStateMachine<OrderSaga, OrderSagaState>().InMemoryRepository();
                x.AddConsumer<PaymentConsumer>();
                x.AddConsumer<InventoryConsumer>();
                x.AddConsumer<ShippingConsumer>();
            })
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        await harness.Bus.Publish(new OrderPlaced(Guid.NewGuid(), "Customer", 100));
        await Task.Delay(2000);
        Assert.True(await harness.GetConsumerHarness<ShippingConsumer>().Consumed.Any<ShipOrderCommand>());
    }

    [Fact]
    public async Task Test9_VerifySagaTransitions()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddSagaStateMachine<OrderSaga, OrderSagaState>().InMemoryRepository();
            })
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var orderId = Guid.NewGuid();
        
        await harness.Bus.Publish(new OrderPlaced(orderId, "Customer", 100));
        await Task.Delay(200);
        harness.GetSagaStateMachineHarness<OrderSaga, OrderSagaState>().Sagas.Contains(orderId)!.CurrentState.Should().Be("PaymentProcessing");

        await harness.Bus.Publish(new PaymentProcessed(orderId));
        await Task.Delay(200);
        harness.GetSagaStateMachineHarness<OrderSaga, OrderSagaState>().Sagas.Contains(orderId)!.CurrentState.Should().Be("InventoryReserving");

        await harness.Bus.Publish(new InventoryReserved(orderId));
        await Task.Delay(200);
        harness.GetSagaStateMachineHarness<OrderSaga, OrderSagaState>().Sagas.Contains(orderId)!.CurrentState.Should().Be("Shipping");
    }

    [Fact]
    public async Task Test10_Compensating_InventoryFailed()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddSagaStateMachine<OrderSaga, OrderSagaState>().InMemoryRepository();
            })
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var orderId = Guid.NewGuid();
        
        await harness.Bus.Publish(new OrderPlaced(orderId, "Customer", 100));
        await Task.Delay(200);
        await harness.Bus.Publish(new PaymentProcessed(orderId));
        await Task.Delay(200);
        await harness.Bus.Publish(new InventoryFailed(orderId, "Stock out"));
        await Task.Delay(200);
        
        var saga = harness.GetSagaStateMachineHarness<OrderSaga, OrderSagaState>().Sagas.Contains(orderId);
        saga.Should().NotBeNull();
        saga!.CurrentState.Should().Be("Failed");
        saga.FailureReason.Should().Be("Stock out");
    }
}

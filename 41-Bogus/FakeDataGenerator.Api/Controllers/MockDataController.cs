using System.Diagnostics;
using FakeDataGenerator.Api.Models;
using FakeDataGenerator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FakeDataGenerator.Api.Controllers;

[ApiController]
[Route("api/mock")]
public class MockDataController : ControllerBase
{
    private readonly IFakeDataService _fakeDataService;

    public MockDataController(IFakeDataService fakeDataService)
    {
        _fakeDataService = fakeDataService;
    }

    /// <summary>
    /// Generates a list of fake user profiles with realistic data and optional seed.
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(List<UserProfile>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetUsers([FromQuery] int count = 10, [FromQuery] int? seed = null, [FromQuery] string locale = "en")
    {
        if (count < 1 || count > 100)
        {
            return BadRequest(new { error = "Count must be between 1 and 100." });
        }

        var users = _fakeDataService.GenerateUsers(count, seed, locale);
        return Ok(users);
    }

    /// <summary>
    /// Generates a list of fake customer orders with items, pricing, and addresses.
    /// </summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(List<CustomerOrder>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetOrders([FromQuery] int count = 5, [FromQuery] int? seed = null)
    {
        if (count < 1 || count > 50)
        {
            return BadRequest(new { error = "Count must be between 1 and 50." });
        }

        var orders = _fakeDataService.GenerateOrders(count, seed);
        return Ok(orders);
    }

    /// <summary>
    /// Generates deterministic customer details based on the specified GUID.
    /// </summary>
    [HttpGet("customers/{id:guid}")]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    public IActionResult GetCustomerById([FromRoute] Guid id)
    {
        var customer = _fakeDataService.GenerateCustomerById(id);
        return Ok(customer);
    }

    /// <summary>
    /// Simulates bulk data seeding for performance testing or development databases.
    /// </summary>
    [HttpPost("seed-db")]
    [ProducesResponseType(typeof(SeedDatabaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SeedDatabase([FromBody] SeedDatabaseRequest request)
    {
        if (request.NumberOfUsers < 1 || request.NumberOfUsers > 1000)
        {
            return BadRequest(new { error = "NumberOfUsers must be between 1 and 1000." });
        }

        if (request.NumberOfOrders < 1 || request.NumberOfOrders > 2000)
        {
            return BadRequest(new { error = "NumberOfOrders must be between 1 and 2000." });
        }

        var sw = Stopwatch.StartNew();

        var users = _fakeDataService.GenerateUsers(request.NumberOfUsers, request.Seed);
        var orders = _fakeDataService.GenerateOrders(request.NumberOfOrders, request.Seed);

        sw.Stop();

        var response = new SeedDatabaseResponse(
            UsersCreated: users.Count,
            OrdersCreated: orders.Count,
            ExecutionTimeMs: sw.ElapsedMilliseconds,
            SampleUser: users.FirstOrDefault(),
            SampleOrder: orders.FirstOrDefault()
        );

        return Ok(response);
    }
}

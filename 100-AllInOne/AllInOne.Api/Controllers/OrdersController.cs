using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AllInOne.Api.Data;
using AllInOne.Api.Models;
using AllInOne.Api.Services;

namespace AllInOne.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly IValidator<CreateOrderRequest> _validator;
    private readonly OrderDataService _dataService;
    private readonly OrderStateMachineService _stateMachineService;
    private readonly OrderDocumentService _documentService;
    private readonly OrderMessagingService _messagingService;

    public OrdersController(
        AppDbContext dbContext,
        IMediator mediator,
        IValidator<CreateOrderRequest> validator,
        OrderDataService dataService,
        OrderStateMachineService stateMachineService,
        OrderDocumentService documentService,
        OrderMessagingService messagingService)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _validator = validator;
        _dataService = dataService;
        _stateMachineService = stateMachineService;
        _documentService = documentService;
        _messagingService = messagingService;
    }

    /// <summary>
    /// 1. Get order dashboard summaries using Dapper (Library 20).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryDto>>> GetOrders()
    {
        var summaries = await _dataService.GetOrderSummariesDapperAsync();
        return Ok(summaries);
    }

    /// <summary>
    /// 2. Get order details by ID using MediatR (Library 04) and EF Core (Library 19).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id));
        if (order == null) return NotFound(new { Error = "Order not found" });

        return Ok(order);
    }

    /// <summary>
    /// 3. Create a new order with FluentValidation (33), EF Core (19), Stateless (49), MassTransit (06), CAP (07), Hangfire (16), OpenTelemetry (40), Serilog (38).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { Errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var entity = new OrderEntity
        {
            OrderNumber = $"ORD-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            Status = OrderStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            Items = request.Items.Select(i => new OrderItemEntity
            {
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        entity.TotalAmount = entity.Items.Sum(i => i.TotalPrice);

        // Stateless (Library 49) - Transition from Draft to Submitted
        _stateMachineService.Transition(entity, OrderTrigger.Submit);

        // EF Core (Library 19)
        _dbContext.Orders.Add(entity);
        await _dbContext.SaveChangesAsync();

        var dto = await _dataService.GetOrderByIdEfAsync(entity.Id);
        if (dto != null)
        {
            // Messaging & Telemetry (MassTransit, CAP, Hangfire, OTel, Serilog, NLog)
            await _messagingService.NotifyOrderLifecycleAsync(dto);
        }

        return CreatedAtAction(nameof(GetOrderById), new { id = entity.Id }, dto);
    }

    /// <summary>
    /// 4. Transition order status using Stateless (Library 49).
    /// </summary>
    [HttpPost("{id}/transition")]
    public async Task<ActionResult<OrderDto>> TransitionOrder(int id, [FromQuery] OrderTrigger trigger)
    {
        var order = await _dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound(new { Error = "Order not found" });

        if (!_stateMachineService.CanTransition(order, trigger))
        {
            return BadRequest(new { Error = $"Cannot fire trigger '{trigger}' from status '{order.Status}'" });
        }

        _stateMachineService.Transition(order, trigger);
        await _dbContext.SaveChangesAsync();

        var dto = await _dataService.GetOrderByIdEfAsync(id);
        return Ok(dto);
    }

    /// <summary>
    /// 5. Run Elsa Code-First Approval Workflow (Library 48).
    /// </summary>
    [HttpPost("{id}/approval-workflow")]
    public async Task<IActionResult> RunApprovalWorkflow(int id)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound(new { Error = "Order not found" });

        // Execute Elsa Code-First approval workflow
        var (isApproved, reason) = _stateMachineService.ExecuteApprovalWorkflow(order.TotalAmount);

        // If approved and status is Submitted, advance state
        if (isApproved && order.Status == OrderStatus.Submitted)
        {
            _stateMachineService.Transition(order, OrderTrigger.Approve);
            await _dbContext.SaveChangesAsync();
        }

        return Ok(new
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Amount = order.TotalAmount,
            IsApproved = isApproved,
            Reason = reason,
            CurrentStatus = order.Status.ToString()
        });
    }

    /// <summary>
    /// 6. Generate official PDF Invoice using QuestPDF (Library 50).
    /// </summary>
    [HttpGet("{id}/invoice-pdf")]
    public async Task<IActionResult> DownloadInvoicePdf(int id)
    {
        var order = await _dataService.GetOrderByIdEfAsync(id);
        if (order == null) return NotFound(new { Error = "Order not found" });

        var bytes = _documentService.GenerateInvoicePdf(order);
        return File(bytes, "application/pdf", $"Invoice-{order.OrderNumber}.pdf");
    }

    /// <summary>
    /// 7. Export orders to styled Excel spreadsheet using ClosedXML (Library 51).
    /// </summary>
    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportExcel()
    {
        var entities = await _dbContext.Orders.Include(o => o.Items).ToListAsync();
        var dtos = entities.Select(e => new OrderDto(
            e.Id, e.OrderNumber, e.CustomerName, e.CustomerEmail,
            e.TotalAmount, e.Status.ToString(), e.CreatedAt,
            e.Items.Select(i => new OrderItemDto(i.Id, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList()
        )).ToList();

        var bytes = _documentService.ExportOrdersExcel(dtos);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OrdersReport.xlsx");
    }

    /// <summary>
    /// 8. Export orders to CSV using CsvHelper (Library 52).
    /// </summary>
    [HttpGet("export-csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var entities = await _dbContext.Orders.Include(o => o.Items).ToListAsync();
        var dtos = entities.Select(e => new OrderDto(
            e.Id, e.OrderNumber, e.CustomerName, e.CustomerEmail,
            e.TotalAmount, e.Status.ToString(), e.CreatedAt,
            e.Items.Select(i => new OrderItemDto(i.Id, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList()
        )).ToList();

        var bytes = _documentService.ExportOrdersCsv(dtos);
        return File(bytes, "text/csv", "Orders.csv");
    }

    /// <summary>
    /// 9. Import orders from CSV using CsvHelper (Library 52).
    /// </summary>
    [HttpPost("import-csv")]
    public IActionResult ImportCsv(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Error = "A non-empty CSV file is required" });

        using var stream = file.OpenReadStream();
        var records = _documentService.ImportOrdersCsv(stream);

        return Ok(new { ImportedCount = records.Count, Records = records });
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Auth;
using StockFlow.Api.Common;
using StockFlow.Api.Data;

namespace StockFlow.Api.Documents;

[ApiController, Route("api/documents"), Authorize]
public sealed class DocumentsController(StockDocumentService documents) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<StockDocumentResponse>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest(Problem("InvalidPage", "Trang phải >= 1 và pageSize nằm trong 1..100."));
        DocumentStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<DocumentStatus>(status, true, out var parsed) || !Enum.IsDefined(parsed))
                return BadRequest(Problem("InvalidStatus", "Trạng thái phiếu không hợp lệ."));
            parsedStatus = parsed;
        }
        var (items, totalCount) = await documents.ListAsync(page, pageSize, parsedStatus, cancellationToken);
        return Ok(new PagedResponse<StockDocumentResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockDocumentResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var document = await documents.GetAsync(id, cancellationToken);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpPost, Authorize(Policy = RolePolicies.WriteInventory)]
    public async Task<ActionResult<StockDocumentResponse>> Create(CreateDocumentRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Type, "Receipt", StringComparison.OrdinalIgnoreCase))
            return BadRequest(Problem("UnsupportedDocumentType", "Task này chỉ tạo phiếu nhập."));
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized();
        try
        {
            var document = await documents.CreateReceiptDraftAsync(request.Id, request.ExpectedVersion, actor,
                new ReceiptDraftPayload(request.SupplierId, request.Lines, request.Reason), cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = document.Id }, document);
        }
        catch (StockFlowOperationException exception) { return ToProblem(exception); }
    }

    [HttpPut("{id:guid}"), Authorize(Policy = RolePolicies.WriteInventory)]
    public async Task<ActionResult<StockDocumentResponse>> Update(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized();
        try
        {
            return Ok(await documents.UpdateReceiptDraftAsync(id, request.ExpectedVersion, actor,
                new ReceiptDraftPayload(request.SupplierId, request.Lines, request.Reason), cancellationToken));
        }
        catch (StockFlowOperationException exception) { return ToProblem(exception); }
    }

    [HttpPost("{id:guid}/post"), Authorize(Policy = RolePolicies.WriteInventory)]
    public async Task<ActionResult<StockDocumentResponse>> Post(Guid id, PostDocumentRequest request, CancellationToken cancellationToken)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized();
        try { return Ok(await documents.PostAsync(id, request.ExpectedVersion, actor, cancellationToken)); }
        catch (StockFlowOperationException exception) { return ToProblem(exception); }
    }

    private ObjectResult ToProblem(StockFlowOperationException exception) => new(new ProblemDetails
    {
        Status = exception.StatusCode,
        Title = exception.Message,
        Extensions = { ["code"] = exception.Code }
    })
    { StatusCode = exception.StatusCode };

    private static ProblemDetails Problem(string code, string title) => new() { Status = StatusCodes.Status400BadRequest, Title = title, Extensions = { ["code"] = code } };
}

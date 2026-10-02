using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Auth;
using StockFlow.Api.Common;

namespace StockFlow.Api.Sync;

[ApiController, Route("api/sync"), Authorize(Policy = RolePolicies.WriteInventory)]
public sealed class SyncController(SyncService sync) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SyncBatchResponse>> Push(SyncBatchRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        try
        {
            var response = await sync.ProcessAsync(userId, request, cancellationToken);
            if (response.Results.Count == 1 && response.Results[0].HttpStatusCode >= 400)
                return ToProblem(response.Results[0]);
            return Ok(response);
        }
        catch (StockFlowOperationException exception)
        {
            return new ObjectResult(new ProblemDetails
            {
                Status = exception.StatusCode,
                Title = exception.Message,
                Extensions = { ["code"] = exception.Code }
            })
            { StatusCode = exception.StatusCode };
        }
    }

    private ObjectResult ToProblem(SyncOperationResult result) => new(new ProblemDetails
    {
        Status = result.HttpStatusCode,
        Title = result.Message,
        Extensions = { ["code"] = result.Code, ["clientOperationId"] = result.ClientOperationId }
    })
    { StatusCode = result.HttpStatusCode };
}

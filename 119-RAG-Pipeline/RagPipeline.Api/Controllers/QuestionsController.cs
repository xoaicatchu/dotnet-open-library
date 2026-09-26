using Microsoft.AspNetCore.Mvc;
using RagPipeline.Api.Models;
using RagPipeline.Api.Rag;

namespace RagPipeline.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController(IRagService ragService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] AskQuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required");

        var response = await ragService.AskAsync(request.Question);
        return Ok(response);
    }
}

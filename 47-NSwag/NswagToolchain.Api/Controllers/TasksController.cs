using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using NswagToolchain.Api.Models;
using NswagToolchain.Api.Services;
using TaskStatus = NswagToolchain.Api.Models.TaskStatus;

namespace NswagToolchain.Api.Controllers;

/// <summary>
/// Quản lý công việc (Task Management) với NSwag OpenAPI Toolchain.
/// </summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
[OpenApiTag("Tasks", Description = "Quản lý tiến độ công việc trong dự án")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Lấy danh sách công việc với bộ lọc trạng thái và độ ưu tiên.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProjectTask>), StatusCodes.Status200OK)]
    public IActionResult GetTasks([FromQuery] TaskStatus? status = null, [FromQuery] TaskPriority? priority = null)
    {
        var tasks = _taskService.GetTasks(status, priority);
        return Ok(tasks);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một công việc theo Id.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectTask), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById([FromRoute] Guid id)
    {
        var task = _taskService.GetTaskById(id);
        if (task == null)
        {
            return NotFound(new { error = $"Task with ID '{id}' was not found." });
        }

        return Ok(task);
    }

    /// <summary>
    /// Tạo mới một công việc trong dự án.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectTask), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateTask([FromBody] CreateTaskRequest request)
    {
        try
        {
            var task = _taskService.CreateTask(request);
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật trạng thái công việc (Backlog, InProgress, Review, Done).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(ProjectTask), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateStatus([FromRoute] Guid id, [FromBody] UpdateTaskStatusRequest request)
    {
        var updated = _taskService.UpdateTaskStatus(id, request.Status);
        if (updated == null)
        {
            return NotFound(new { error = $"Task with ID '{id}' was not found." });
        }

        return Ok(updated);
    }

    /// <summary>
    /// Xóa một công việc theo Id.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteTask([FromRoute] Guid id)
    {
        if (!_taskService.DeleteTask(id))
        {
            return NotFound(new { error = $"Task with ID '{id}' was not found." });
        }

        return NoContent();
    }
}

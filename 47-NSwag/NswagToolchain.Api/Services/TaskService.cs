using System.Collections.Concurrent;
using NswagToolchain.Api.Models;
using TaskStatus = NswagToolchain.Api.Models.TaskStatus;

namespace NswagToolchain.Api.Services;

public interface ITaskService
{
    List<ProjectTask> GetTasks(TaskStatus? status = null, TaskPriority? priority = null);
    ProjectTask? GetTaskById(Guid id);
    ProjectTask CreateTask(CreateTaskRequest request);
    ProjectTask? UpdateTaskStatus(Guid id, TaskStatus newStatus);
    bool DeleteTask(Guid id);
}

public class TaskService : ITaskService
{
    private readonly ConcurrentDictionary<Guid, ProjectTask> _tasks = new();

    public TaskService()
    {
        var id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        _tasks[id1] = new ProjectTask(
            Id: id1,
            Title: "Implement OAuth2 Authentication",
            Description: "Configure OpenIddict with Client Credentials and Authorization Code flow.",
            Priority: TaskPriority.High,
            Status: TaskStatus.InProgress,
            AssigneeEmail: "dev@company.com",
            DueDate: DateTime.UtcNow.AddDays(7),
            CreatedAt: DateTime.UtcNow.AddDays(-2),
            UpdatedAt: DateTime.UtcNow.AddDays(-1)
        );

        var id2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        _tasks[id2] = new ProjectTask(
            Id: id2,
            Title: "Setup CI/CD Pipeline",
            Description: "Configure GitHub Actions workflow for automated testing and deployment.",
            Priority: TaskPriority.Medium,
            Status: TaskStatus.Done,
            AssigneeEmail: "devops@company.com",
            DueDate: DateTime.UtcNow.AddDays(-1),
            CreatedAt: DateTime.UtcNow.AddDays(-5),
            UpdatedAt: DateTime.UtcNow.AddDays(-1)
        );
    }

    public List<ProjectTask> GetTasks(TaskStatus? status = null, TaskPriority? priority = null)
    {
        var query = _tasks.Values.AsEnumerable();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        return query.OrderByDescending(t => t.CreatedAt).ToList();
    }

    public ProjectTask? GetTaskById(Guid id)
    {
        _tasks.TryGetValue(id, out var task);
        return task;
    }

    public ProjectTask CreateTask(CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(request.Title));
        }

        var id = Guid.NewGuid();
        var task = new ProjectTask(
            Id: id,
            Title: request.Title,
            Description: request.Description,
            Priority: request.Priority,
            Status: TaskStatus.Backlog,
            AssigneeEmail: request.AssigneeEmail,
            DueDate: request.DueDate,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _tasks[id] = task;
        return task;
    }

    public ProjectTask? UpdateTaskStatus(Guid id, TaskStatus newStatus)
    {
        if (!_tasks.TryGetValue(id, out var existing))
        {
            return null;
        }

        var updated = existing with
        {
            Status = newStatus,
            UpdatedAt = DateTime.UtcNow
        };

        _tasks[id] = updated;
        return updated;
    }

    public bool DeleteTask(Guid id)
    {
        return _tasks.TryRemove(id, out _);
    }
}

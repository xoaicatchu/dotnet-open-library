using System.Collections.Concurrent;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Data;

public class TaskStore
{
    private readonly ConcurrentDictionary<int, TaskItem> _tasks = new();
    private int _nextId = 2;

    public TaskStore()
    {
        var task1 = new TaskItem
        {
            Id = 1,
            Title = "Setup development environment",
            IsCompleted = true,
            CreatedAt = DateTime.UtcNow
        };
        var task2 = new TaskItem
        {
            Id = 2,
            Title = "Write unit tests",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _tasks.TryAdd(task1.Id, task1);
        _tasks.TryAdd(task2.Id, task2);
    }

    public IEnumerable<TaskItem> GetAll() => _tasks.Values.OrderBy(t => t.Id);

    public TaskItem? GetById(int id) => _tasks.TryGetValue(id, out var task) ? task : null;

    public TaskItem Add(TaskItem task)
    {
        task.Id = Interlocked.Increment(ref _nextId);
        task.CreatedAt = DateTime.UtcNow;
        _tasks.TryAdd(task.Id, task);
        return task;
    }

    public bool Update(TaskItem task)
    {
        if (!_tasks.ContainsKey(task.Id)) return false;
        _tasks[task.Id] = task;
        return true;
    }

    public bool Delete(int id) => _tasks.TryRemove(id, out _);
}

using Carter;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using TaskBoard.Api.Data;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Modules;

public class TaskModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Tasks");

        group.MapGet("/", (TaskStore store, bool? completed) =>
        {
            var tasks = store.GetAll();
            if (completed.HasValue)
            {
                tasks = tasks.Where(t => t.IsCompleted == completed.Value);
            }
            return Results.Ok(tasks);
        });

        group.MapGet("/{id:int}", (int id, TaskStore store) =>
        {
            var task = store.GetById(id);
            return task is not null ? Results.Ok(task) : Results.NotFound();
        });

        group.MapPost("/", (CreateTaskRequest req, TaskStore store, IValidator<CreateTaskRequest> validator) =>
        {
            var validationResult = validator.Validate(req);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var task = new TaskItem
            {
                Title = req.Title,
                Description = req.Description
            };
            var created = store.Add(task);
            return Results.Created($"/api/tasks/{created.Id}", created);
        });

        group.MapPut("/{id:int}", (int id, UpdateTaskRequest req, TaskStore store, IValidator<UpdateTaskRequest> validator) =>
        {
            var validationResult = validator.Validate(req);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var existing = store.GetById(id);
            if (existing is null) return Results.NotFound();

            existing.Title = req.Title;
            existing.Description = req.Description;
            existing.IsCompleted = req.IsCompleted;

            store.Update(existing);
            return Results.Ok(existing);
        });

        group.MapDelete("/{id:int}", (int id, TaskStore store) =>
        {
            var deleted = store.Delete(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        group.MapPatch("/{id:int}/complete", (int id, TaskStore store) =>
        {
            var existing = store.GetById(id);
            if (existing is null) return Results.NotFound();

            existing.IsCompleted = true;
            store.Update(existing);
            return Results.Ok(existing);
        });
    }
}

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

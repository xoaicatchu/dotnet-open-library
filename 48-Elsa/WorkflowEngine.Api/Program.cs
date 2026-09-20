using Elsa.Extensions;
using WorkflowEngine.Api.Services;
using WorkflowEngine.Api.Workflows;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Elsa services
builder.Services.AddElsa(elsa =>
{
    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.AddWorkflow<GreetingWorkflow>();
        runtime.AddWorkflow<OrderApprovalWorkflow>();
    });
});

builder.Services.AddScoped<WorkflowService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();

public partial class Program;

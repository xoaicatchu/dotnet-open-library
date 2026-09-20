using ContactManager.Api.Behaviors;
using ContactManager.Api.Data;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddSingleton<ContactStore>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler(appError => appError.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (ex is ValidationException ve)
    {
        context.Response.StatusCode = 400;
        var errors = ve.Errors.GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key.ToLower(), g => g.Select(e => e.ErrorMessage).ToArray());
        await context.Response.WriteAsJsonAsync(new { errors });
    }
}));

if (app.Environment.IsDevelopment()) 
{ 
    app.UseSwagger(); 
    app.UseSwaggerUI(); 
}

app.MapControllers();

app.Run();

public partial class Program { }

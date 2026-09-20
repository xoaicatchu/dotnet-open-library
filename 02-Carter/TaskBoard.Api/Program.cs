using Carter;
using FluentValidation;
using TaskBoard.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCarter();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSingleton<TaskStore>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapCarter();
app.Run();

public partial class Program { }

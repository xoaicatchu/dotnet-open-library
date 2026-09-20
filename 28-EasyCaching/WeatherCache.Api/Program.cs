using EasyCaching.Core.Configurations;
using EasyCaching.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configure EasyCaching with In-Memory provider
builder.Services.AddEasyCaching(options =>
{
    options.UseInMemory("default_mem");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program;

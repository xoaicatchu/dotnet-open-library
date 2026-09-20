using FastEndpoints;
using FastEndpoints.Swagger;
using ProductCatalog.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ProductStore>();
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

var app = builder.Build();

app.UseFastEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.Run();

public partial class Program;

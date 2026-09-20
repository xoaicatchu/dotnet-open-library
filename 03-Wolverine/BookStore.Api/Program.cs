using BookStore.Api.Data;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWolverine();
builder.Services.AddSingleton<BookStore.Api.Data.BookStore>();
builder.Services.AddControllers();
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

public partial class Program { }

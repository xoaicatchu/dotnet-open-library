using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using OrderMapping.Api.Data;
using OrderMapping.Api.Mappings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Register AutoMapper
var mapperConfig = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<OrderMappingProfile>();
}, NullLoggerFactory.Instance);
mapperConfig.AssertConfigurationIsValid();
builder.Services.AddSingleton(mapperConfig.CreateMapper());

builder.Services.AddSingleton<OrderStore>();

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

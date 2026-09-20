using OrderService.Data;
using OrderService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<OrderDbContext>(opt => opt.UseSqlite("Data Source=orders.db"));

var productServiceUrl = builder.Configuration["ProductService:GrpcUrl"] ?? "http://localhost:5311";
builder.Services.AddGrpcClient<ProductGrpc.ProductGrpcClient>(o => o.Address = new Uri(productServiceUrl));
builder.Services.AddScoped<IProductServiceClient, ProductServiceGrpcClient>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapControllers();
app.Run();
public partial class Program {}

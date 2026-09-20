using EventSourcing.Api.EventStore;
using EventSourcing.Api.Handlers.Commands;
using EventSourcing.Api.Handlers.Queries;
using EventSourcing.Api.ReadModel;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<EventStoreDbContext>(opt =>
    opt.UseSqlite("Data Source=eventstore.db"));
builder.Services.AddScoped<IEventStore, SqliteEventStore>();
builder.Services.AddSingleton<IProductReadRepository, InMemoryProductReadRepository>();
builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<UpdateProductHandler>();
builder.Services.AddScoped<DeactivateProductHandler>();
builder.Services.AddScoped<GetProductsHandler>();
builder.Services.AddScoped<GetProductByIdHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }

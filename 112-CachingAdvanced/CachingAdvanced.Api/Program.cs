using Microsoft.EntityFrameworkCore;
using CachingAdvanced.Api.Data;
using CachingAdvanced.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018
builder.Services.AddDistributedMemoryCache();
builder.Services.AddFusionCache();
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=caching.db"));
builder.Services.AddScoped<MemoryCacheService>();
builder.Services.AddScoped<DistributedCacheService>();
builder.Services.AddScoped<HybridCacheService>();
builder.Services.AddScoped<FusionCacheService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
public partial class Program {}

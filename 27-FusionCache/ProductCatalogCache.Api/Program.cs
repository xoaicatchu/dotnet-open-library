using ProductCatalogCache.Api.Services;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Register FusionCache with memory cache as L1
builder.Services.AddMemoryCache();
builder.Services.AddFusionCache()
    .WithSerializer(new FusionCacheSystemTextJsonSerializer())
    .WithDefaultEntryOptions(options =>
    {
        options.Duration = TimeSpan.FromMinutes(2);
        options.IsFailSafeEnabled = true;
        options.FailSafeMaxDuration = TimeSpan.FromHours(1);
        options.FailSafeThrottleDuration = TimeSpan.FromSeconds(5);
        options.FactorySoftTimeout = TimeSpan.FromMilliseconds(100);
    });

builder.Services.AddSingleton<IProductCatalogService, ProductCatalogService>();

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

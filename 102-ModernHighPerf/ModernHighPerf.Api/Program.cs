using Savorboard.CAP.InMemoryMessageQueue;
using Serilog;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.MemoryStorage;
using Mapster;
using Wolverine;
using ZiggyCreatures.Caching.Fusion;
using ModernHighPerf.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console().MinimumLevel.Information());

builder.Host.UseWolverine(opts => {
    // Wolverine discovers handlers by convention automatically
});

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=modern.db"));

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddFusionCache();

builder.Services.AddCap(x => {
    x.UseInMemoryStorage();
    x.UseInMemoryMessageQueue();
});

builder.Services.AddHangfire(c => c.UseMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.AddOpenApiDocument(cfg => cfg.Title = "ModernHighPerf API"); // NSwag
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    AppDbContext.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.MapControllers();
app.Run();

public partial class Program;

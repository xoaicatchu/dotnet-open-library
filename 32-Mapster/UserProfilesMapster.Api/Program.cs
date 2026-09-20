using Mapster;
using UserProfilesMapster.Api.Data;
using UserProfilesMapster.Api.Mappings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configure Mapster global settings
MapsterConfig.RegisterMappings(TypeAdapterConfig.GlobalSettings);

builder.Services.AddSingleton<UserStore>();

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

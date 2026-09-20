using Refit;
using RefitClientDemo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var externalUrl = builder.Configuration["ExternalServices:UsersApiUrl"] ?? "https://api.example.com";

// Register Refit client with HttpClientFactory
builder.Services.AddRefitClient<IUsersApiClient>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri(externalUrl);
        c.Timeout = TimeSpan.FromSeconds(15);
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

using NotificationService.Api.Abstractions;
using NotificationService.Api.Decorators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Scrutor assembly scanning
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.AssignableTo<INotificationSender>()
                                  .NotInNamespaceOf<LoggingNotificationSender>())
        .AsImplementedInterfaces()
        .WithTransientLifetime()
    .AddClasses(classes => classes.AssignableTo<IMessageFormatter>())
        .AsImplementedInterfaces()
        .WithSingletonLifetime());

// Scrutor decoration
builder.Services.Decorate<INotificationSender, LoggingNotificationSender>();
builder.Services.Decorate<INotificationSender, RetryNotificationSender>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapControllers();
app.Run();

public partial class Program { }

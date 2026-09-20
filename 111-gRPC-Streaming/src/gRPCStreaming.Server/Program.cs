using gRPCStreaming.Server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc(options => {
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16MB
    options.MaxSendMessageSize = 16 * 1024 * 1024;
});
builder.Services.AddGrpcReflection(); // tool support

var app = builder.Build();

app.MapGrpcReflectionService();
app.MapGrpcService<ProductStreamService>();
app.MapGrpcService<UploadStreamService>();
app.MapGrpcService<ChatStreamService>();
app.MapGet("/", () => "gRPC Streaming Server running. Use gRPC client to connect.");

app.Run();
public partial class Program { }

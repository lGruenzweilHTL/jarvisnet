using System.Reflection;
using AssistantCore.Tools;
using AssistantCore.Voice;
using AssistantCore.Workers;
using AssistantCore.Chat;
using AssistantCore.Logging;
using AssistantCore.Middleware;
using AssistantCore.Workers.LoadBalancing;

var builder = WebApplication.CreateBuilder(args);

// Configure logging: console (default) and a simple file logger provider
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var logDir = builder.Configuration.GetValue<string>("LogDirectory") ?? "logs";
var logPath = Path.Combine(logDir, $"assistantcore-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");
var fileProvider = new FileLoggerProvider(logPath);
builder.Services.AddSingleton<ILoggerProvider>(fileProvider);

builder.Services.AddControllers();

builder.Services.AddSingleton(provider =>
{
    var logger = provider.GetService<ILogger<ToolCollector>>();
    var tc = new ToolCollector(logger!);
    tc.SetAssemblies([Assembly.GetExecutingAssembly()]);
    return tc;
});
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton(provider => ChatManager.Create(TimeSpan.FromMinutes(30)));
builder.Services.AddSingleton<SatelliteManager>();
builder.Services.AddSingleton<WorkerRegistry>();
builder.Services.AddSingleton<ILoadBalancer, RoundRobinLoadBalancer>();

builder.Services.AddSingleton<ISttWorkerClient, HttpSttWorkerClient>();
builder.Services.AddSingleton<IRoutingWorkerClient, HttpRoutingWorkerClient>();
builder.Services.AddSingleton<ILlmWorkerClient, HttpLlmWorkerClient>();
builder.Services.AddSingleton<ITtsWorkerClient, HttpTtsWorkerClient>();

var app = builder.Build();

// Register middleware for exception logging and request logging
app.UseMiddleware<ExceptionLoggingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// Register system prompts from configuration using the host logger
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
var promptsSection = builder.Configuration.GetSection("LLMConfig");
LlmInfo.ParseConfig(promptsSection, logger);

// Configure WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});
app.MapControllers();

// Ensure we cancel any active voice pipelines when host is shutting down
app.Lifetime.ApplicationStopping.Register(() =>
{
    var mgr = app.Services.GetRequiredService<SatelliteManager>();
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    startupLogger.LogInformation("Application stopping: cancelling active satellite pipelines");
    mgr.CancelAllPipelines();
});

// Https redirection is not supported for WebSocket connections
//app.UseHttpsRedirection();


app.Run();

using System.Reflection;
using AssistantCore.Tools;
using AssistantCore.Voice;
using AssistantCore.Workers;
using AssistantCore.Workers.Impl;
using AssistantCore.Chat;
using AssistantCore.Logging;
using AssistantCore.Middleware;

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
builder.Services.AddSingleton<ILlmWorkerFactory, LlmWorkerFactory>();
builder.Services.AddSingleton<ISttWorker, DummyStt>();
builder.Services.AddSingleton<IRoutingWorker, DummyRouter>();
builder.Services.AddSingleton<ILlmWorker, DummyLlm>();
builder.Services.AddSingleton<ITtsWorker, DummyTts>();
builder.Services.AddSingleton(provider => ChatManager.Create(TimeSpan.FromMinutes(30)));
builder.Services.AddSingleton<SatelliteManager>();

var app = builder.Build();

// Register middleware for exception logging and request logging
app.UseMiddleware<ExceptionLoggingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// Register system prompts from configuration using the host logger
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
var promptsSection = builder.Configuration.GetSection("SystemPrompts");
if (promptsSection.Exists())
{
    var dict = new Dictionary<LlmSpeciality, string>();
    foreach (var child in promptsSection.GetChildren())
    {
        var key = child.Key;
        var value = child.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            logger.LogWarning("Skipping empty system prompt for key '{Key}'", key);
            continue;
        }

        if (!Enum.TryParse<LlmSpeciality>(key, ignoreCase: true, out var speciality))
        {
            logger.LogWarning("Unknown LlmSpeciality key in SystemPrompts configuration: '{Key}'", key);
            continue;
        }

        dict[speciality] = value;
        logger.LogInformation("Loaded system prompt for speciality {Speciality}", speciality);
    }

    try
    {
        SystemPromptRegistry.RegisterAll(dict, overwrite: true);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to register system prompts at startup");
        throw;
    }
}


// Configure WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});
app.MapControllers();
app.Map("/ws/satellite", (Action<IApplicationBuilder>)(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var manager = context.RequestServices.GetRequiredService<SatelliteManager>();
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString();
        var connLogger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger($"SatelliteConnection-{connectionId}");
        connLogger.LogInformation("Accepted new satellite connection {ConnectionId}", connectionId);
        var connection = SatelliteConnection.Create(connectionId, socket, connLogger);
        manager.RegisterConnection(connection);
        try
        {
            // Link the HttpContext request cancellation with the host shutdown token so connection
            // RunAsync exits promptly when the application is stopping.
            var hostLifetime = context.RequestServices.GetRequiredService<IHostApplicationLifetime>();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, hostLifetime.ApplicationStopping);
            await connection.RunAsync(linkedCts.Token);
        }
        finally
        {
            manager.UnregisterConnection(connection);
        }
    });
}));

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

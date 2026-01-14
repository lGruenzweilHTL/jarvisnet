using AssistantCore.Tools;
using AssistantCore.Voice;
using AssistantCore.Workers;
using AssistantCore.Workers.Impl;
using AssistantCore.Chat;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddSingleton<ToolCollector>();
builder.Services.AddSingleton<ILlmWorkerFactory, LlmWorkerFactory>();
builder.Services.AddSingleton<ISttWorker, DummyStt>();
builder.Services.AddSingleton<IRoutingWorker, DummyRouter>();
builder.Services.AddSingleton<ILlmWorker, DummyLlm>();
builder.Services.AddSingleton<ITtsWorker, DummyTts>();
builder.Services.AddSingleton(provider => ChatManager.Create(TimeSpan.FromMinutes(30)));
builder.Services.AddSingleton<SatelliteManager>();

// Register system prompts from configuration
var logger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("Startup");
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

var app = builder.Build();

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
        var connection = SatelliteConnection.Create(connectionId, socket);
        manager.RegisterConnection(connection);
        try
        {
            await connection.RunAsync(context.RequestAborted);
        }
        finally
        {
            manager.UnregisterConnection(connection);
        }
    });
}));

// Https redirection is not supported for WebSocket connections
//app.UseHttpsRedirection();


app.Run();
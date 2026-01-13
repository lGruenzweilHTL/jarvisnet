using AssistantCore.Tools;
using AssistantCore.Voice;
using AssistantCore.Workers;
using AssistantCore.Workers.Impl;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();

builder.Services.AddSingleton<ToolCollector>();
builder.Services.AddSingleton<ILlmWorkerFactory, LlmWorkerFactory>();
builder.Services.AddSingleton<ISttWorker, DummyStt>();
builder.Services.AddSingleton<IRoutingWorker, DummyRouter>();
builder.Services.AddSingleton<ILlmWorker, DummyLlm>();
builder.Services.AddSingleton<ITtsWorker, DummyTts>();
builder.Services.AddSingleton<SatelliteManager>();

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
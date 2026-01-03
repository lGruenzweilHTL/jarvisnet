using AssistantCore.Voice;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
// TODO: add services for python workers (stt, router, llm, tts)

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString();
        var connection = new SatelliteConnection(connectionId, socket);
        SatelliteManager.Instance.RegisterConnection(connection);
    });
}));

// Https redirection is not supported for WebSocket connections
//app.UseHttpsRedirection();


app.Run();
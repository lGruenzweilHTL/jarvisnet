using AssistantCore.Voice;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

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
        await SocketHandler.HandleSatelliteConnection(socket, context);
    });
}));

app.UseHttpsRedirection();


app.Run();
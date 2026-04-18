var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.UseUrls("http://0.0.0.0:5295");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// Device WebSocket at "/" — intercepted before UseDefaultFiles rewrites to /index.html
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && context.WebSockets.IsWebSocketRequest)
    {
        var ws = await context.WebSockets.AcceptWebSocketAsync();
        await Test.Services.SensorHandler.HandleDevice(ws);
    }
    else
    {
        await next(context);
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();

// Client WebSocket — receives live sensor data broadcasts
app.Map("/sensor", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var ws = await context.WebSockets.AcceptWebSocketAsync();
        await Test.Services.SensorHandler.HandleClient(ws);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});


app.MapControllers();

app.Run();

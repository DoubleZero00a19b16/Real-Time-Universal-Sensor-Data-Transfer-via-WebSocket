using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Test.Services;

public static class SensorHandler
{
    private static readonly List<WebSocket> _clients = new();
    private static string? _lastMessage = null;
    private static readonly HttpClient _http = new();

    private static readonly string? _n8nUrl =
        Environment.GetEnvironmentVariable("N8N_WEBHOOK_URL");

    private static readonly Channel<string> _queue =
        Channel.CreateUnbounded<string>();

    static SensorHandler()
    {
        Task.Run(async () =>
        {
            await foreach (var payload in _queue.Reader.ReadAllAsync())
            {
                if (string.IsNullOrEmpty(_n8nUrl)) continue;
                try
                {
                    using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    var response = await _http.PostAsync(_n8nUrl, content);
                    Console.WriteLine($"[n8n] → {(int)response.StatusCode} {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[n8n] Forward error: {ex.Message}");
                }
            }
        });
    }

    public static async Task HandleDevice(WebSocket webSocket)
    {
        Console.WriteLine("[Device] Connected");
        var buffer = new byte[4096];

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                try
                {
                    JsonSerializer.Deserialize<JsonElement>(message);
                    _lastMessage = message;
                    Console.WriteLine($"[Data] {message}");
                    await Broadcast(message);
                    _queue.Writer.TryWrite(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Device] Parse error: {ex.Message}");
                }
            }
        }
        finally
        {
            Console.WriteLine("[Device] Disconnected");
        }
    }

    public static async Task HandleClient(WebSocket webSocket)
    {
        _clients.Add(webSocket);

        if (_lastMessage != null)
            await Send(webSocket, _lastMessage);

        try
        {
            var buffer = new byte[256];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }
            }
        }
        finally
        {
            _clients.Remove(webSocket);
        }
    }

    private static async Task Broadcast(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var dead = new List<WebSocket>();

        foreach (var client in _clients)
        {
            if (client.State == WebSocketState.Open)
            {
                try { await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); }
                catch { dead.Add(client); }
            }
            else dead.Add(client);
        }

        foreach (var d in dead) _clients.Remove(d);
    }

    private static async Task Send(WebSocket ws, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        try { await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); }
        catch { }
    }
}

using Newtonsoft.Json.Linq;
using SharedLib;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace ServerClientShared;

public class WebClient
{
    private const int WAIT_TIME = 60;
    private readonly WebSocket _ws;

    public bool IsAlive => _ws.IsAlive;
    public string Status => _ws.ReadyState.ToString();

    private readonly string? _path;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly CancellationToken _cancellationToken;

    public event Action<string>? OnStringMessage;
    public event Action<byte[]>? OnDataMessage;

    public WebClient(string path, string? ip = "127.0.0.1", ushort port = 8080)
    {
        _path = path;

        _ws = new WebSocket($"ws://{ip}:{port}/{_path}");
        _ws.WaitTime = TimeSpan.FromSeconds(WAIT_TIME);

        _ws.OnOpen += OnOpen;
        _ws.OnMessage += OnMessage;
        _ws.OnError += OnError;
        _ws.OnClose += OnClose;

        _cancellationTokenSource = new CancellationTokenSource();
        _cancellationToken = _cancellationTokenSource.Token;
        PingLoop();
    }

    public async Task Connect()
    {
        await Task.Run(() =>
        {
            _ws.Connect();
        });
    }

    public void Close()
    {
        _ws.Close();
        _cancellationTokenSource.Cancel();
    }

    private async void PingLoop()
    {
        // wait for connection to be established before starting the ping loop
        while (!IsAlive)
            await Task.Delay(10, _cancellationToken);

        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                _ws.Ping();
                await Task.Delay(TimeSpan.FromSeconds(10), _cancellationToken);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"PingLoop cancelled [{_path}]");
            }
        }
    }

    private void OnOpen(object? sender, EventArgs eventArgs)
    {
        Console.WriteLine($"[WebSocket Opened]: {_path}");
    }

    private void OnMessage(object? sender, MessageEventArgs e)
    {
        Console.WriteLine($"[WebSocket Message] {e.Data}");

        if (e.IsText)
            OnStringMessage?.Invoke(e.Data);
        else if (e.IsBinary)
            OnDataMessage?.Invoke(e.RawData);
    }

    private void OnError(object? sender, ErrorEventArgs e)
    {
        Console.WriteLine($"[WebSocket Error] {e.Message}");
    }

    private void OnClose(object? sender, CloseEventArgs e)
    {
        Console.WriteLine($"[WebSocket Close ({e.Code})] {e.Reason}");
    }

    public void Send(object data)
    {
        var json = Json.Serialise(data);
        _ws.Send(json);
    }

    public void SendJObject(JObject data)
    {
        _ws.Send(data.ToString());
    }

    private async void SendInternalAsync(string data)
    {
        while (_ws.ReadyState != WebSocketState.Open)
        {
            Console.WriteLine($"Waiting for socket [{_ws.ReadyState}]: {_path}");
            await Task.Delay(500);
        }

        _ws.Send(data);
    }
}

using MainServer.Client;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace MainServer.Offloads;

internal class OffloadServer
{
    public bool IsAlive => _connector.IsAlive;

    private readonly string _ip;
    private readonly ushort _port;
    private readonly WebSocket _connector;
    
    public string OperatingSystem { get; private set; } = string.Empty;
    public Dictionary<string, WebClientBase> Services { get; } = new();
    
    public delegate void BuildCompleteDelegate(string targetName, long buildTime);
    public event BuildCompleteDelegate? OnBuildCompleteMessage;

    public event Action<byte[]>? OnDataMessage;
    
    public OffloadServer(string ip, ushort port)
    {
        _ip = ip;
        _port = port;
        
        _connector = new WebSocket($"ws://{ip}:{port}/connect");
        _connector.WaitTime = TimeSpan.FromSeconds(60);
       
        _connector.OnOpen += (sender, args) 
            => Console.WriteLine("Connected to offload server");
        _connector.OnClose += RetryConnection;
        _connector.OnMessage += OnConnectionMessage;
        
        _connector.Connect();
    }
    
    private async void RetryConnection(object? sender, CloseEventArgs closeEventArgs)
    {
        Console.WriteLine("Disconnected from offload server");

        while (!_connector.IsAlive)
        {
            // ReSharper disable once MethodHasAsyncOverload
            _connector.Connect();
            await Task.Delay(1000);
        }
    }

    private void OnConnectionMessage(object? sender, MessageEventArgs e)
    {
        if (e.IsBinary)
        {
            OnDataMessage?.Invoke(e.RawData);
            return;
        }
        
        var res = JObject.Parse(e.Data);
        var status = res["Status"]?.ToObject<string>() ?? throw new NullReferenceException();

        switch (status)
        {
            case "Connection":
                // connect to the services
                OperatingSystem = res[nameof(OperatingSystem)]?.ToString() ?? throw new NullReferenceException();
                var services = res[nameof(Services)]?.ToObject<List<string>>() ?? throw new NullReferenceException();
                SetupServices(services);
                break;
            
            case "Building":
                break;
            
            case "Complete":
                var targetName = res["TargetName"]?.ToString() ?? throw new NullReferenceException();
                var buildTime = res["Time"]?.Value<long>() ?? 0;
                OnBuildCompleteMessage?.Invoke(targetName, buildTime);
                break;
            
            case "Error":
                Console.WriteLine($"Error: {res["Message"]}");
                break;
        }
    }
    
    private async void SetupServices(List<string> inServices)
    {
        var connections = new List<Task>();
        foreach (var service in inServices)
        {
            var client = new WebClientBase(service, _ip, _port);
            Services.Add(service, client);
            
            var connection = client.Connect();
            connections.Add(connection);
        }
        
        await Task.WhenAll(connections);
        Console.WriteLine("Connected to offload servers: ");
        foreach (var inService in inServices)
            Console.WriteLine($"- {inService}");
    }
    
    public void Send(string service, string message)
    {
        if (Services.TryGetValue(service, out var offloader))
            offloader.Send(message);
    }
}
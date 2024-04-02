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
    
    public OffloadServer(string ip, ushort port)
    {
        _ip = ip;
        _port = port;
        
        _connector = new WebSocket($"ws://{ip}:{port}/connect");
        _connector.WaitTime = TimeSpan.FromSeconds(60);
       
        _connector.OnOpen += (sender, args) 
            => Console.WriteLine("Connected to offload server");
        _connector.OnClose += (sender, args) 
            => Console.WriteLine("Disconnected from offload server");
        _connector.OnMessage += OnConnectionMessage;
        
        _connector.Connect();
    }
    
    private async void RetryConnection()
    {
        while (!_connector.IsAlive)
        {
            // ReSharper disable once MethodHasAsyncOverload
            _connector.Connect();
            await Task.Delay(1000);
        }
    }

    private void OnConnectionMessage(object? sender, MessageEventArgs e)
    {
        // connect to the services
        var res = JObject.Parse(e.Data);
        OperatingSystem = res[nameof(OperatingSystem)]?.ToString() ?? throw new NullReferenceException();
        var services = res[nameof(Services)]?.ToObject<List<string>>() ?? throw new NullReferenceException();
        SetupServices(services);
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
        Console.WriteLine("Connected to offload server");
    }

    // public async void Connect()
    // {
    //     // get the available services
    //     var task = new TaskCompletionSource<JObject>();
    //     _connector.OnMessage += (sender, args) 
    //         => task.SetResult(JObject.Parse(args.Data));
    //
    //     // ReSharper disable once MethodHasAsyncOverload
    //     _connector.Connect();
    //     var res = await task.Task;
    //     
    //     // connect to the services
    //     OperatingSystem = res[nameof(OperatingSystem)]?.ToString() ?? throw new NullReferenceException();
    //     var services = res[nameof(Services)]?.ToObject<List<string>>() ?? throw new NullReferenceException();
    //
    //     var connections = new List<Task>();
    //     foreach (var service in services)
    //     {
    //         var client = new WebClientBase(service, ip, port);
    //         Services.Add(service, client);
    //         var connection = client.Connect();
    //         connections.Add(connection);
    //     }
    //
    //     await Task.WhenAll(connections);
    //     Console.WriteLine("Connected to offload server");
    // }
}
using MainServer.Client;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace MainServer.Offloads;

internal class OffloadServer(string ip, ushort port)
{
    public string OperatingSystem { get; private set; } = string.Empty;
    public Dictionary<string, WebClientBase> Services { get; } = new();

    public async void Connect()
    {
        using var connector = new WebSocket($"ws://{ip}:{port}/connect");

        connector.OnError += (sender, args) 
            => Console.WriteLine(args.Exception);
        
        // get the available services
        var task = new TaskCompletionSource<JObject>();
        connector.OnMessage += (sender, args) 
            => task.SetResult(JObject.Parse(args.Data));

        // ReSharper disable once MethodHasAsyncOverload
        connector.Connect();
        var res = await task.Task;
        
        // connect to the services
        OperatingSystem = res[nameof(OperatingSystem)]?.ToString() ?? throw new NullReferenceException();
        var services = res[nameof(Services)]?.ToObject<List<string>>() ?? throw new NullReferenceException();

        var connections = new List<Task>();
        foreach (var service in services)
        {
            var client = new WebClientBase(service, ip, port);
            Services.Add(service, client);
            var connection = client.Connect();
            connections.Add(connection);
        }

        await Task.WhenAll(connections);
        
        // ReSharper disable once MethodHasAsyncOverload
        connector.Close();
        Console.WriteLine("Connected to offload server");
    }
}
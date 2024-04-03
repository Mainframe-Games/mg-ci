using MainServer.Configs;
using MainServer.Offloads;
using MainServer.Services;
using MainServer.Utils;
using WebSocketSharp.Server;

Console.Title = $"Main Server - {ServerInfo.Version}";

var configPath = GetArg("-config", args);
var config = !string.IsNullOrEmpty(configPath) 
    ? ServerConfig.Load(configPath)
    : ServerConfig.Load();

var mainServer = StartMainServer(config.Ip, config.Port);

OffloadServerManager.Init(config.Offloaders);

Console.ReadLine();
mainServer.Stop();

Console.WriteLine("---- End of program ----");
return;

static WebSocketServer StartMainServer(string ip, ushort port)
{
    var server = new WebSocketServer($"ws://{ip}:{port}");
    server.AddWebSocketService<BuildService>("/build");
    // server.AddWebSocketService<ReportService>("/report");
    server.Start();

    Console.WriteLine($"Server started on {server.Address}:{server.Port}");

    if (server.IsListening)
    {
        Console.WriteLine("Listening on port {0}, and providing WebSocket services:", server.Port);

        foreach (var path in server.WebSocketServices.Paths)
            Console.WriteLine("- {0}", path);
    }

    return server;
}

static string? GetArg(string arg, string[] args)
{
    var index = Array.IndexOf(args, arg);
    if (index == -1 || index + 1 >= args.Length)
    {
        return null;
    }

    return args[index + 1];
}
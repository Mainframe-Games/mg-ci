using Server;
using Server.Configs;
using Server.Services;
using SharedLib;
using WebSocketSharp.Server;

try
{
    Console.Title = $"Build Server - {App.Version}";

    var config = ServerConfig.Load();

    var server = new WebSocketServer($"ws://{config.IP}:{config.Port}");
    server.AddWebSocketService<TestService>("/test");
    server.AddWebSocketService<BuildService>("/build");
    server.AddWebSocketService<ReportService>("/report");
    server.Start();

    Console.WriteLine($"Server started on {server.Address}:{server.Port}");

    if (server.IsListening)
    {
        Console.WriteLine("Listening on port {0}, and providing WebSocket services:", server.Port);

        foreach (var path in server.WebSocketServices.Paths)
            Console.WriteLine("- {0}", path);
    }

    Console.WriteLine("\nPress Enter key to stop the server...");
    
    Console.ReadLine();
    server.Stop();
}
catch (Exception e)
{
    Logger.Log(e);
}

Console.WriteLine("---- End of program ----");
Console.Read();

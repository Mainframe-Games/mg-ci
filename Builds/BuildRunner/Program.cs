using BuildRunner;
using WebSocketSharp.Server;

/*
 * The job of the BuildRunnerService is to listen for incoming build requests from the server.
 * It should know which engine build to run based on in coming packet
 */

var server = new WebSocketServer("ws://localhost:8081");
server.AddWebSocketService<BuildRunnerService>("/start-build");

server.Start();

Console.WriteLine($"Server started on {server.Address}:{server.Port}");

if (server.IsListening)
{
    Console.WriteLine($"Listening on port {server.Port}, and providing WebSocket services:");

    foreach (var path in server.WebSocketServices.Paths)
        Console.WriteLine($"- {path}");
}

Console.WriteLine("\nPress Enter key to stop the server...");

Console.ReadLine();
server.Stop();

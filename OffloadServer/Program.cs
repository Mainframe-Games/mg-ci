using OffloadServer;
using OffloadServer.Utils;
using WebSocketSharp.Server;

/*
 * The job of the Offload server is to listen for incoming build requests from the server.
 * It should know which engine build to run based on in coming packet
 *
 * - Builds (Unity, Godot)
 * - Deployments for iOS
 */

var ip = Arg.GetArg("-ip") ?? "127.0.0.1";
var port = ushort.Parse(Arg.GetArg("-port") ?? "8081");

var server = new WebSocketServer($"ws://{ip}:{port}");
server.AddWebSocketService<VersionBumpService>("/connect");
server.AddWebSocketService<VersionBumpService>("/version-bump");
server.AddWebSocketService<BuildRunnerService>("/build");

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
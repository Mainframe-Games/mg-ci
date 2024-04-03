using OffloadServer;
using Tomlyn;
using WebSocketSharp.Server;

/*
 * The job of the Offload server is to listen for incoming build requests from the server.
 * It should know which engine build to run based on in coming packet
 *
 * - Builds (Unity, Godot)
 * - Deployments for iOS
 */

var tomlStr = File.ReadAllText("/Users/broganking/ci-cache/Unity Test/.ci/project.toml");
var toml = Toml.ToModel(tomlStr);

var ip = GetArg("-ip", args) ?? "127.0.0.1";
var port = ushort.Parse(GetArg("-port", args) ?? "8081");

var server = new WebSocketServer($"ws://{ip}:{port}");
server.AddWebSocketService<ConnectService>("/connect");
ConnectService.Services = new[]{"version-bump", "build"};
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
return ;

static string? GetArg(string name, IReadOnlyList<string> args)
{
    for (int i = 0; i < args.Count; i++)
    {
        if (args[i] == name && args.Count > i + 1)
            return args[i + 1];
    }

    return null;
}
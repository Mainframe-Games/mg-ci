using MainServer;
using MainServer.Configs;
using MainServer.Services.Server;
using MainServer.Utils;
using SocketServer;
using Tomlyn;

Console.Title = $"Main Server - {ServerInfo.Version}";

var serverConfigPath = GetArg("-config", args);
var serverConfig = !string.IsNullOrEmpty(serverConfigPath)
    ? LoadConfig(serverConfigPath)
    : LoadConfig();

Server server;

if (args.Contains("-runner"))
{
    // start build runner server
    server = new Server((ushort)(serverConfig.Port + 1));
    server.AddService(new BuildRunnerServerService(server, serverConfig));
}
else
{
    // start main server
    server = new Server(serverConfig.Port);
    server.AddService(new BuildServerService(server, serverConfig));

    ClientServicesManager.Init(serverConfig.Runners);
}

if (OperatingSystem.IsMacOS())
    server.AddService(new XcodeServerService(server));

server.Start();

Console.ReadLine();
Console.WriteLine("---- End of program ----");
return;

static string? GetArg(string arg, string[] args)
{
    var index = Array.IndexOf(args, arg);
    if (index == -1 || index + 1 >= args.Length)
        return null;
    return args[index + 1];
}

static ServerConfig LoadConfig(string path = "config-server.toml")
{
    if (!File.Exists(path))
        File.WriteAllText(path, Toml.FromModel(new ServerConfig()));

    var configStr = File.ReadAllText(path);
    Console.WriteLine($"Loading Config: {new FileInfo(path).FullName}");
    // Console.WriteLine("#### Server Config Start ####");
    // Console.WriteLine(configStr);
    // Console.WriteLine("#### Server Config End ####");
    return Toml.ToModel<ServerConfig>(configStr);
}

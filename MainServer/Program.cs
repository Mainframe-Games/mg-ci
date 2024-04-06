using MainServer;
using MainServer.Configs;
using MainServer.Services.Server;
using MainServer.Utils;

Console.Title = $"Main Server - {ServerInfo.Version}";

if (args.Contains("-runner"))
{
    // start build runner server
    var portStr = GetArg("-runner", args) ?? "8081";
    var server = new SocketServer.Server(ushort.Parse(portStr));
    server.AddService(new BuildRunnerServerService(server));
    server.Start();
}
else
{
    // start main server
    var configPath = GetArg("-config", args);
    var config = !string.IsNullOrEmpty(configPath)
        ? ServerConfig.Load(configPath)
        : ServerConfig.Load();

    // start server
    var server = new SocketServer.Server(config.Port);
    server.AddService(new BuildServerService(server));
    server.Start();

    BuildRunnerManager.Init(config.Runners);
}

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

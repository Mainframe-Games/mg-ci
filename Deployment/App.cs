using Deployment.Server;
using Deployment.Server.Config;
using SharedLib;

namespace Deployment;

public static class App
{
	public static string RootDirectory { get; set; }
	public static ListenServer? Server { get; set; }
	
	public static async Task RunAsync(string[] args)
	{
		var config = ServerConfig.Load();
		RootDirectory = Environment.CurrentDirectory;

		if (config.RunServer || Args.Environment.IsFlag("-server", false))
		{
			Args.Environment.TryGetArg("-server", 0, out string ip, config.IP);
			Args.Environment.TryGetArg("-server", 1, out int port, config.Port);
			Server = new ListenServer(ip, (ushort)port);

			if (config.AuthTokens is { Count: > 0 })
			{
				Server.GetAuth = () =>
				{
					config.Refresh();
					return config.AuthTokens;
				};
			}
		
			// server should wait for ever
			await Server.RunAsync();
			Logger.Log("Server stopped");
		}
		else
		{
			var currentWorkspace = Workspace.GetWorkspace();
			Logger.Log($"Chosen workspace: {currentWorkspace}");
			var pipe = new BuildPipeline(currentWorkspace, args);
			await pipe.RunAsync();
		}
	}

	public static void DumpLogs()
	{
		Logger.WriteToFile(RootDirectory, true);
		Server?.CheckIfServerStillListening();
		Environment.CurrentDirectory = RootDirectory; // reset cur dir back to root of exe
	}
}
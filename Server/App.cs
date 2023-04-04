using Deployment;
using Deployment.Configs;
using Deployment.Deployments;
using Server.Configs;
using Server.RemoteBuild;
using SharedLib;

namespace Server;

public static class App
{
	private static string RootDirectory { get; set; }
	private static ListenServer? Server { get; set; }
	
	public static async Task RunAsync(string[]? args)
	{
		var config = ServerConfig.Load();
		RootDirectory = Environment.CurrentDirectory;

		// for locally running the build process without a listen server
		if (Args.Environment.IsFlag("-local"))
		{
			var workspace = Workspace.GetWorkspace();
			await RunBuildPipe(workspace, args);
		}
		else
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
	}

	public static void DumpLogs()
	{
		Logger.WriteToFile(RootDirectory, true);
		Server?.CheckIfServerStillListening();
		Environment.CurrentDirectory = RootDirectory; // reset cur dir back to root of exe
	}

	#region Build Pipeline
	
	public static async Task RunBuildPipe(Workspace workspace, string[]? args)
	{
		var pipe = new BuildPipeline(workspace, args, ServerConfig.Instance.OffloadServerUrl);
		pipe.OffloadBuildNeeded += RemoteBuildTargetRequest.SendRemoteBuildRequest;
		pipe.GetExtraHookLogs += BuildPipelineOnGetExtraHookLog;
		pipe.DeployEvent += BuildPipelineOnDeployEvent;
		await pipe.RunAsync();
		DumpLogs();
	}

	private static async Task BuildPipelineOnDeployEvent(DeployContainer deploy, string buildVersionTitle)
	{
		// steam
		if (deploy.Steam != null)
		{
			foreach (var vdfPath in deploy.Steam)
			{
				var path = ServerConfig.Instance.Steam.Path;
				var password = ServerConfig.Instance.Steam.Password;
				var username = ServerConfig.Instance.Steam.Username;
				var steam = new SteamDeploy(vdfPath, password, username, path);
				steam.Deploy(buildVersionTitle);
			}
		}

		// clanforge
		if (deploy.Clanforge == true)
		{
			var clanforge = new ClanForgeDeploy(ServerConfig.Instance.Clanforge, buildVersionTitle);
			await clanforge.Deploy();
		}
	}

	private static string? BuildPipelineOnGetExtraHookLog()
	{
		return ServerConfig.Instance.Clanforge?.BuildHookMessage("Updated");
	}
	
	#endregion
}
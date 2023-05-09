using Deployment;
using Deployment.Configs;
using Deployment.Deployments;
using Server.Configs;
using Server.RemoteBuild;
using SharedLib;

namespace Server;

public static class App
{
	private static string? RootDirectory { get; set; }
	private static ListenServer? Server { get; set; }
	private static ServerConfig? Config { get; set; }
	
	private static bool IsLocal { get; set; }
	
	public static async Task RunAsync(string[]? args)
	{
		Config = ServerConfig.Load();
		RootDirectory = Environment.CurrentDirectory;
		
		Logger.Log($"Config: {Config}");

		IsLocal = Args.Environment.IsFlag("-local");

		// for locally running the build process without a listen server
		if (IsLocal)
		{
			var workspace = Workspace.GetWorkspace();
			await RunBuildPipe(workspace, args);
		}
		else
		{
			Args.Environment.TryGetArg("-server", 0, out string ip, Config.IP);
			Args.Environment.TryGetArg("-server", 1, out int port, Config.Port);
			Server = new ListenServer(ip, (ushort)port);
			
			if (Config.AuthTokens is { Count: > 0 })
			{
				Server.GetAuth = () =>
				{
					Config.Refresh();
					return Config.AuthTokens;
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
		var pipe = new BuildPipeline(workspace, args, Config.OffloadServerUrl, Config.OffloadTargets);
		pipe.OffloadBuildNeeded += SendRemoteBuildRequest;
		pipe.GetExtraHookLogs += BuildPipelineOnGetExtraHookLog;
		pipe.DeployEvent += BuildPipelineOnDeployEvent;
		var isSuccessful = await pipe.RunAsync();
		if (isSuccessful)
			DumpLogs();
	}
	
	/// <summary>
	/// Called from main build server. Sends web request to offload server and gets a buildId in return
	/// </summary>
	private static void SendRemoteBuildRequest(OffloadServerPacket offloadPacket)
	{
		var remoteBuild = new RemoteBuildTargetRequest
		{
			SendBackUrl = $"http://{Config.IP}:{Config.Port}",
			Packet = offloadPacket,
		};
		
		var body = new RemoteBuildPacket { BuildTargetRequest = remoteBuild };
		Web.SendAsync(HttpMethod.Post, Config.OffloadServerUrl, body: body).FireAndForget();
	}

	private static async Task BuildPipelineOnDeployEvent(BuildPipeline pipeline)
	{
		var buildVersionTitle = pipeline.BuildVersionTitle;
		
		// steam
		if (pipeline.Config.Deploy.Steam != null)
		{
			foreach (var vdfPath in pipeline.Config.Deploy.Steam)
			{
				var path = Config.Steam.Path;
				var password = Config.Steam.Password;
				var username = Config.Steam.Username;
				var steam = new SteamDeploy(vdfPath, password, username, path);
				steam.Deploy(buildVersionTitle);
			}
		}
		
		// google store
		if (pipeline.Config.Deploy.GoogleStore != null)
		{
			var packageName = pipeline.Workspace.ProjectSettings.GetProjPropertyValue("applicationIdentifier", "Android");
			var changeLogArr = pipeline.GetChangeLog();
			var changeLog = string.Join("\n", changeLogArr);
			var androidBuild = pipeline.Config.Builds.First(x => x.Target == UnityTarget.Android);
			var aabFile = new DirectoryInfo(androidBuild.BuildPath).GetFiles("*.aab").First().FullName;
			
			await GooglePlayDeploy.Deploy(
				packageName,
				aabFile,
				Config.GoogleStore.CredentialsPath,
				Config.GoogleStore.ServiceUsername,
				buildVersionTitle,
				changeLog);
		}
		
		// apple store
		if (pipeline.Config.Deploy.AppleStore == true)
			XcodeDeploy.Deploy(Config.AppleStore.AppleId, Config.AppleStore.AppSpecificPassword);

		// clanforge
		if (pipeline.Config.Deploy.Clanforge == true)
		{
			var clanforge = new ClanForgeDeploy(Config.Clanforge, buildVersionTitle);
			await clanforge.Deploy();
		}
	}

	private static string? BuildPipelineOnGetExtraHookLog()
	{
		return Config.Clanforge?.BuildHookMessage("Updated");
	}
	
	#endregion
}
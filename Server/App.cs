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

	public static ulong NextPipelineId { get; private set; }
	public static readonly Dictionary<ulong, BuildPipeline> Pipelines = new();

	public static async Task RunAsync(string[]? args)
	{
		Config = ServerConfig.Load();
		RootDirectory = Environment.CurrentDirectory;
		IsLocal = Args.Environment.IsFlag("-local");

		// for locally running the build process without a listen server
		if (IsLocal)
		{
			var workspace = Workspace.AskWorkspace();
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
		var parallel = Config?.Offload?.Parallel ?? false;
		var targets = Config?.Offload?.Targets ?? null;
		
		var pipe = new BuildPipeline(NextPipelineId++, workspace, args, Config?.OffloadServerUrl, parallel, targets);
		Pipelines.Add(pipe.Id, pipe);
		
		pipe.OffloadBuildNeeded += SendRemoteBuildRequest;
		pipe.GetExtraHookLogs += BuildPipelineOnGetExtraHookLog;
		pipe.DeployEvent += BuildPipelineOnDeployEvent;
		
		var isSuccessful = await pipe.RunAsync();
		if (isSuccessful)
			DumpLogs();
		
		Pipelines.Remove(pipe.Id);
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
		await DeployToS3Bucket(pipeline);
		DeploySteam(pipeline, buildVersionTitle);
		await DeployGoogle(pipeline, buildVersionTitle);
		DeployApple(pipeline);
		await DeployClanforge(pipeline, buildVersionTitle);
	}

	private static async Task DeployToS3Bucket(BuildPipeline pipeline)
	{
		if (Config?.S3 == null)
			return;
		
		var pathToBuild = pipeline.Config.GetBuildTarget(UnityTarget.Linux64, true).BuildPath;
		var s3 = new AmazonS3Deploy(Config.S3.AccessKey, Config.S3.SecretKey, Config.S3.BucketName);
		await s3.DeployAsync(pathToBuild);
	}

	private static async Task DeployClanforge(BuildPipeline pipeline, string buildVersionTitle)
	{
		if (pipeline.Config.Deploy?.Clanforge is null)
			return;

		var profile = pipeline.Config.Deploy.Clanforge.Profile;
		pipeline.Args.TryGetArg("-clanforgebeta", out var branch);
		var clanforge = new ClanForgeDeploy(Config.Clanforge, profile, buildVersionTitle, branch);
		await clanforge.Deploy();
	}

	private static void DeployApple(BuildPipeline pipeline)
	{
		if (pipeline.Config.Deploy == null || pipeline.Config.Deploy.AppleStore is null or false)
			return;
		
		var iosBuild = pipeline.Config.GetBuildTarget(UnityTarget.iOS);
		var buildSettingsAsset = iosBuild.GetBuildSettingsAsset(pipeline.Workspace.BuildSettingsDirPath);
		var productName = buildSettingsAsset.GetValue<string>("ProductName");
		var buildPath = buildSettingsAsset.GetValue<string>("BuildPath");
		var workingDir = Path.Combine(buildPath, productName);
		var exportOptionPlist = $"{pipeline.Workspace.Directory}/BuildScripts/ios/exportOptions.plist";

		if (!File.Exists(exportOptionPlist))
			throw new FileNotFoundException(exportOptionPlist);

		XcodeDeploy.Deploy(
			workingDir,
			Config.AppleStore.AppleId,
			Config.AppleStore.AppSpecificPassword,
			exportOptionPlist);
	}

	private static async Task DeployGoogle(BuildPipeline pipeline, string buildVersionTitle)
	{
		if (pipeline.Config.Deploy?.GoogleStore == null)
			return;
		
		var packageName = pipeline.Workspace.ProjectSettings.GetValue<string>("applicationIdentifier.Android");
		var changeLogArr = pipeline.GetChangeLog();
		var changeLog = string.Join("\n", changeLogArr);
		var androidBuild = pipeline.Config.GetBuildTarget(UnityTarget.Android);
		var buildSettingsAsset = androidBuild.GetBuildSettingsAsset(pipeline.Workspace.BuildSettingsDirPath);
		var productName = buildSettingsAsset.GetValue<string>("ProductName");
		var buildPath = buildSettingsAsset.GetValue<string>("BuildPath");
		var path = Path.Combine(buildPath, $"{productName}.aab");
		var aabFile = new FileInfo(path);

		if (!aabFile.Exists)
			throw new FileNotFoundException($"aab file not found: {path}");

		await GooglePlayDeploy.Deploy(
			packageName,
			aabFile.FullName,
			Config.GoogleStore.CredentialsPath,
			Config.GoogleStore.ServiceUsername,
			buildVersionTitle,
			changeLog);
	}

	private static void DeploySteam(BuildPipeline pipeline, string buildVersionTitle)
	{
		var vdfPaths = pipeline.Config.Deploy?.Steam;
		pipeline.Args.TryGetArg("-setlive", out var setLive);
		
		if (vdfPaths == null)
			return;
		
		foreach (var vdfPath in vdfPaths)
		{
			var path = Config.Steam.Path;
			var password = Config.Steam.Password;
			var username = Config.Steam.Username;
			var steam = new SteamDeploy(vdfPath, password, username, path);
			steam.Deploy(buildVersionTitle, setLive);
		}
	}

	private static string? BuildPipelineOnGetExtraHookLog(string? profile)
	{
		return Config.Clanforge?.BuildHookMessage(profile, "Updated");
	}
	
	#endregion
}
using Deployment.ChangeLogBuilders;
using Deployment.Configs;
using Deployment.Deployments;
using Deployment.PreBuild;
using Deployment.RemoteBuild;
using Deployment.Server;
using Deployment.Webhooks;
using SharedLib;

namespace Deployment;

public class BuildPipeline
{
	public static BuildPipeline? Current { get; private set; }

	private readonly Args _args;
	
	private LocalUnityBuild _unity;
	private BuildConfig _config;
	private PreBuildBase _preBuild;

	public Workspace Workspace { get; }

	private string BuildVersionTitle => $"Build Version: {_preBuild?.BuildVersion}";

	public BuildPipeline(Workspace workspace, string[]? args)
	{
		Workspace = workspace;
		_args = new Args(args);
		Environment.CurrentDirectory = workspace.Directory;
		Current = this;
	}
	
	#region Build Steps
	
	public async Task RunAsync()
	{
		var startTime = DateTime.Now;
		await Prebuild();
		await Build();
		await DeployAsync();
		await PostBuild();
		Logger.Log($"Deployed. {DateTime.Now - startTime:hh\\:mm\\:ss}");
	}

	private async Task Prebuild()
	{
		if (_args.IsFlag("-noprebuild"))
			return;

		Logger.Log("PreBuild process started...");
		
		Workspace.Clear();
		if(_args.TryGetArg("-changeSetId", 0, out int id))
			Workspace.Update(id);
		
		_config = GetConfigJson(Workspace.Directory);
		_preBuild = PreBuildBase.Create(_config.PreBuild?.Type ?? default);
		_preBuild.Run();
		
		if (_config.PreBuild?.ChangeLog == true)
			_preBuild.SetChangeLog();
		
		await Task.CompletedTask;
	}
	
	private async Task Build()
	{
		if (_args.IsFlag("-nobuild"))
			return;
		
		_unity = new LocalUnityBuild(Workspace.UnityVersion);

		if (_config == null || _unity == null || _config.Builds == null)
			throw new NullReferenceException();
		
		Logger.Log("Build process started...");

		// configs
		foreach (var build in _config.Builds)
		{
			// Build
			if (IsOffload(build))
				await _unity.SendRemoteBuildRequest(Workspace.Name, _preBuild.CurrentChangeSetId, build, ServerConfig.Instance.OffloadServerUrl);
			else
				await _unity.Build(build);
		}
		
		await _unity.WaitBuildIds();
	}

	/// <summary>
	/// Returns if offload is needed for IL2CPP
	/// <para></para>
	/// NOTE: Linux IL2CPP target can be built from Mac and Windows 
	/// </summary>
	/// <param name="target"></param>
	/// <returns></returns>
	private static bool IsOffload(TargetConfig target)
	{
		if (string.IsNullOrEmpty(ServerConfig.Instance.OffloadServerUrl))
			return false;
		
		// mac server
		if (OperatingSystem.IsMacOS())
			return target.Target is UnityTarget.Win64;
		
		// windows server
		if (OperatingSystem.IsWindows())
			return target.Target is UnityTarget.OSXUniversal;

		// linux server
		return target.Target is UnityTarget.Win64 or UnityTarget.OSXUniversal;
	}

	private async Task DeployAsync()
	{
		if (_args.IsFlag("-nodeploy"))
			return;
		
		if(_config.Deploy == null)
			return;

		if (_config.Deploy.Steam != null)
		{
			foreach (var steamConfig in _config.Deploy.Steam)
			{
				var steam = new SteamDeploy(steamConfig);
				steam.Deploy(BuildVersionTitle);
			}
		}
		
		if (_config.Deploy.Multiplay != null)
		{
			var multiplay = new MultiplayDeploy();
			await multiplay.Deploy(_config.Deploy.Multiplay.Ccd.PathToBuild);
		}

		await Task.CompletedTask;
	}
	
	private async Task PostBuild()
	{
		if (_args.IsFlag("-nopostbuild"))
			return;

		Logger.Log("PostBuild process started...");

		if (_preBuild?.IsRun ?? false)
			_preBuild.CommitNewVersionNumber();
		
		if (_config?.Hooks == null)
			return;
		
		var commits = _preBuild?.ChangeLog;
		var hooks = _config.Hooks;

		foreach (var hook in hooks)
		{
			if (hook.IsDiscord())
			{
				var discord = new ChangeLogBuilderDiscord();
				discord.BuildLog(commits);
				Discord.PostMessage(hook.Url, discord.ToString(), hook.Title, BuildVersionTitle, Discord.Colour.GREEN);
			}
			else if (hook.IsSlack())
			{
				Slack.PostMessage(hook.Url, $"{hook.Title} | {BuildVersionTitle}");
			}
		}

		await Task.CompletedTask;
	}
	
	#endregion

	#region Helper Methods

	private static BuildConfig GetConfigJson(string? workingDirectory)
	{
		if (workingDirectory == null)
			return new BuildConfig();
		
		var path = Path.Combine(workingDirectory, "BuildScripts", "buildconfig.json");
		var configStr = File.ReadAllText(path);
		var configClass = Json.Deserialise<BuildConfig>(configStr);

		if (configClass == null)
			throw new NullReferenceException("Failed to parse buildconfig.json");
		
		return configClass;
	}

	public async Task RemoteBuildReceived(RemoteBuildResponse remoteBuildResponse)
	{
		await _unity.RemoteBuildReceived(remoteBuildResponse);
	}
	
	#endregion

}
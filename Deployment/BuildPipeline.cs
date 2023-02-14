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
	
	private readonly Workspace _workspace;
	private readonly Args _args;
	
	private LocalUnityBuild _unity;
	private BuildConfig _config;
	private PreBuildBase _preBuild;

	public Workspace Workspace => _workspace;

	private string BuildVersionTitle => $"Build Version: {_preBuild?.BuildVersion}";

	public BuildPipeline(Workspace workspace, string[]? args)
	{
		_workspace = workspace;
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
		Cmd.Run("cm", "unco -a"); // clear workspace
		Cmd.Run("cm", "upd"); // update workspace
		_config = GetConfigJson(_workspace.Directory);
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
		
		_unity = new LocalUnityBuild(_workspace.UnityVersion);

		if (_config == null || _unity == null || _config.Builds == null)
			throw new NullReferenceException();
		
		Logger.Log("Build process started...");

		// configs
		foreach (var build in _config.Builds)
		{
			// Build
			if (IsOffload(build))
				await _unity.SendRemoteBuildRequest(_workspace.Name, build,
					ServerConfig.Instance.OffloadServerUrl,
					ServerConfig.Instance.MasterServerUrl);
			else
				await _unity.Build(build);
		}
		
		await _unity.WaitBuildIds();
		
		if (_preBuild?.IsRun ?? false)
			_preBuild.CommitNewVersionNumber();
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
		
		if (_config?.Hooks == null)
			return;
		
		Logger.Log("PostBuild process started...");

		var hooks = _config.Hooks;
		var commits = _preBuild.ChangeLog;

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

	private static BuildConfig GetConfigJson(string workingDirectory)
	{
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
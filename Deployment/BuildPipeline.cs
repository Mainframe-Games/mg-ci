using System.Text;
using Deployment.ChangeLogBuilders;
using Deployment.Configs;
using Deployment.Deployments;
using Deployment.PreBuild;
using Deployment.RemoteBuild;
using Deployment.Server.Config;
using Deployment.Webhooks;
using SharedLib;

namespace Deployment;

public class BuildPipeline
{
	public static BuildPipeline? Current { get; private set; }
	public static event Action? OnCompleted; 

	private readonly Args _args;
	
	private LocalUnityBuild _unity;
	private BuildConfig _config;
	private PreBuildBase _preBuild;

	public Workspace Workspace { get; }
	private DateTime StartTime { get; set; }
	private string TimeSinceStart => $"{DateTime.Now - StartTime:hh\\:mm\\:ss}";

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
		try
		{
			StartTime = DateTime.Now;
			await Prebuild();
			await Build();
			await DeployAsync();
			await PostBuild();
			Logger.Log($"Pipeline Completed. {TimeSinceStart}");
			OnCompleted?.Invoke();
		}
		catch (Exception e)
		{
			Logger.Log(e);
			SendErrorHook(e);
		}
		
		Current = null;
	}

	private async Task Prebuild()
	{
		_config = GetConfigJson(Workspace.Directory); // need to get config even if -noprebuild flag
		
		if (_args.IsFlag("-noprebuild"))
			return;

		Logger.Log("PreBuild process started...");

		if (_args.IsFlag("-cleanbuild"))
			Workspace.CleanBuild();
		
		Workspace.Clear();
		_args.TryGetArg("-changeSetId", 0, out int id);
		Workspace.Update(id);
		
		_config = GetConfigJson(Workspace.Directory); // refresh config
		_preBuild = PreBuildBase.Create(_config.PreBuild?.Type ?? default);
		_preBuild.Run();
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
			bool success;
			
			// Build
			if (IsOffload(build))
			{
				success = await _unity.SendRemoteBuildRequest(Workspace.Name, 
					_preBuild.CurrentChangeSetId,
					_preBuild.BuildVersion,
					build, 
					ServerConfig.Instance.OffloadServerUrl,
					_args.IsFlag("-cleanbuild"));
			}
			else
			{
				success = await _unity.Build(build);
			}
			
			if (!success)
				throw new Exception("Build Failure");
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
		
		if (_config.Deploy == null)
			return;

		// steam
		if (_config.Deploy.Steam != null)
		{
			foreach (var vdfPath in _config.Deploy.Steam)
			{
				var steam = new SteamDeploy(vdfPath, ServerConfig.Instance.Steam);
				steam.Deploy(BuildVersionTitle);
			}
		}

		// clanforge
		if (_config.Deploy.Clanforge == true)
		{
			var clanforge = new ClanForgeDeploy(ServerConfig.Instance.Clanforge, BuildVersionTitle);
			await clanforge.Deploy();
		}

		await Task.CompletedTask;
	}
	
	private async Task PostBuild()
	{
		if (_args.IsFlag("-nopostbuild"))
			return;

		Logger.Log("PostBuild process started...");
		
		// collect change logs
		var commits = _config.PreBuild?.ChangeLog == true
			? _preBuild?.GetChangeLog()
			: Array.Empty<string>();
		
		// committing new version must be done after collecting changeLogs as the prev changesetid will be updated
		_preBuild?.CommitNewVersionNumber();
		
		if (_config.Hooks == null)
			return;

		// optional message from clanforge
		var clanforgeMessage = _config.Deploy?.Clanforge == true 
			? ServerConfig.Instance.Clanforge?.BuildHookMessage("Updated")
			: string.Empty;
		
		foreach (var hook in _config.Hooks)
		{
			if (hook.IsErrorChannel)
				continue;
			
			if (hook.IsDiscord())
			{
				var discord = new ChangeLogBuilderDiscord();
				discord.BuildLog(commits);
				var hookMessage = new StringBuilder();
				hookMessage.AppendLine($"Total Time: {TimeSinceStart}");
				hookMessage.AppendLine(clanforgeMessage);
				hookMessage.AppendLine(discord.ToString());
				
				Discord.PostMessage(hook.Url, hookMessage.ToString(), hook.Title, BuildVersionTitle, Discord.Colour.GREEN);
			}
			else if (hook.IsSlack())
			{
				var hookMessage = new StringBuilder();
				hookMessage.AppendLine($"{hook.Title} | {BuildVersionTitle}");
				hookMessage.AppendLine($"Total Time: {TimeSinceStart}");
				hookMessage.AppendLine(clanforgeMessage);
				Slack.PostMessage(hook.Url, hookMessage.ToString());
			}
		}
		
		Workspace.Clear();

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

	private void SendErrorHook(Exception e)
	{
		if(_config.Hooks == null)
			return;
		
		var hookMessage = new StringBuilder();
		
		foreach (var hook in _config.Hooks)
		{
			if (!hook.IsErrorChannel)
				continue;
			
			hookMessage.Clear();
				
			if (hook.IsDiscord())
			{
				hookMessage.AppendLine(hook.Title);
				hookMessage.AppendLine(e.ToString());
				Discord.PostMessage(hook.Url, hookMessage.ToString(), hook.Title, BuildVersionTitle, Discord.Colour.RED);
			}
			else if (hook.IsSlack())
			{
				hookMessage.AppendLine(hook.Title);
				hookMessage.AppendLine(e.ToString());
				Slack.PostMessage(hook.Url, hookMessage.ToString());
			}
		}
	}

	#endregion

}
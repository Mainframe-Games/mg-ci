using System.Net;
using System.Text;
using Builder;
using Builds.PreBuild;
using Deployment.Configs;
using SharedLib;
using SharedLib.ChangeLogBuilders;
using SharedLib.Webhooks;

namespace Deployment;

public class BuildPipeline
{
	public static BuildPipeline? Current { get; private set; }

	public delegate Task<string> OffloadBuildReqPacket(string workspaceName, int changesetId, string buildVersion, TargetConfig targetConfig, string offloadUrl, bool cleanBuild);
	public delegate string? ExtraHookLogs();
	public delegate Task DeployDelegate(DeployContainer deploy, string buildVersionTitle);
	
	public event OffloadBuildReqPacket OffloadBuildNeeded;
	public event ExtraHookLogs GetExtraHookLogs;
	public event DeployDelegate DeployEvent;

	private readonly Args _args;
	private readonly string _offloadUrl;
	private BuildConfig _config;

	public Workspace Workspace { get; }
	private DateTime StartTime { get; set; }
	private string TimeSinceStart => $"{DateTime.Now - StartTime:hh\\:mm\\:ss}";
	private string BuildVersionTitle => $"Build Version: {_buildVersion}";

	/// <summary>
	/// The change set id that was current when build started
	/// </summary>
	private int _currentChangeSetId;
	private int _previousChangeSetId;
	private string _buildVersion;

	/// <summary>
	/// build ids we are waiting for offload server
	/// </summary>
	private readonly List<string> _buildIds = new();

	public BuildPipeline(Workspace workspace, string[]? args, string? offloadUrl)
	{
		Workspace = workspace;
		_args = new Args(args);
		_offloadUrl = offloadUrl;
		Environment.CurrentDirectory = workspace.Directory;
		Current = this;
	}
	
	#region Build Steps

	public async Task RunAsync()
	{
		try
		{
			StartTime = DateTime.Now;
			await PingOffloadServer();
			await Prebuild();
			await Build();
			await DeployAsync();
			await PostBuild();
			Logger.LogTimeStamp("Pipeline Completed", StartTime);
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
		_config = BuildConfig.GetConfig(Workspace.Directory); // need to get config even if -noprebuild flag
		
		if (_args.IsFlag("-noprebuild"))
			return;

		Logger.Log("PreBuild process started...");

		if (_args.IsFlag("-cleanbuild"))
			Workspace.CleanBuild();
		
		Workspace.Clear();
		_args.TryGetArg("-changeSetId", 0, out int id);
		Workspace.Update(id);

		_currentChangeSetId = Workspace.GetCurrentChangeSetId();
		_previousChangeSetId = Workspace.GetPreviousChangeSetId();
		Logger.Log($"[CHANGESET] cs:{_previousChangeSetId} \u2192 cs:{_currentChangeSetId}");
		
		_config = BuildConfig.GetConfig(Workspace.Directory); // refresh config

		var buildType = _config.PreBuild?.Type ?? default;
		var preBuild = PreBuildBase.Create(buildType, Workspace);
		preBuild.Run();
		_buildVersion = preBuild.BuildVersion;
		await Task.CompletedTask;
	}
	
	private async Task Build()
	{
		if (_args.IsFlag("-nobuild"))
			return;
		
		if (_config?.Builds == null)
			throw new NullReferenceException();
		
		await ClonesManager.CloneProject(Workspace.Directory, _config);
		Logger.Log("Build process started...");
		var buildStartTime = DateTime.Now;
		
		var tasks = new List<Task>();

		foreach (var build in _config.Builds)
		{
			var targetPath = ClonesManager.GetTargetPath(Workspace.Directory, build);
			var unity = new LocalUnityBuild(Workspace.UnityVersion);
			
			if (unity == null || _config?.Builds == null)
				throw new NullReferenceException();
			
			// offload build
			if (IsOffload(build))
			{
				// TODO: need to make this an array
				var buildId = await OffloadBuildNeeded.Invoke(
					Workspace.Name,
					_currentChangeSetId,
					_buildVersion,
					build,
					_offloadUrl,
					_args.IsFlag("-cleanbuild")
				);
				_buildIds.Add(buildId);
			}
			// local build
			else
			{
				build.BuildPath = Path.Combine(Workspace.Directory, build.BuildPath);
				var task = Task.Run(() => unity.Build(targetPath, build));
				tasks.Add(task);
			}
		}

		var isSuccess = true;
		
		foreach (var task in tasks)
			task.WaitAndThrow(_ => isSuccess = false);

		if (!isSuccess)
			throw new Exception("Build Failed");
		
		await WaitBuildIds();
		Logger.LogTimeStamp("Build time", buildStartTime);
		ClonesManager.Cleanup();
	}

	/// <summary>
	/// Returns if offload is needed for IL2CPP
	/// <para></para>
	/// NOTE: Linux IL2CPP target can be built from Mac and Windows 
	/// </summary>
	/// <param name="target"></param>
	/// <returns></returns>
	private bool IsOffload(TargetConfig target)
	{
		if (string.IsNullOrEmpty(_offloadUrl))
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
		
		await DeployEvent.Invoke(_config.Deploy, BuildVersionTitle);
	}
	
	private async Task PostBuild()
	{
		if (_args.IsFlag("-nopostbuild"))
			return;

		Logger.Log("PostBuild process started...");
		
		// collect change logs
		var commits = _config.PostBuild?.ChangeLog == true
			? Workspace.GetChangeLog(_currentChangeSetId, _previousChangeSetId)
			: Array.Empty<string>();
		
		// committing new version must be done after collecting changeLogs as the prev changesetid will be updated
		Workspace.CommitNewVersionNumber(_currentChangeSetId, _buildVersion);
		
		if (_config.Hooks == null)
			return;

		// optional message from clanforge
		var clanforgeMessage = _config.Deploy?.Clanforge == true 
			? GetExtraHookLogs?.Invoke()
			: null;

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

	/// <summary>
	/// Pings Offload server to check if its awake
	/// </summary>
	/// <exception cref="WebException"></exception>
	private async Task PingOffloadServer()
	{
		// ignore if no offload server is needed
		if (string.IsNullOrEmpty(_offloadUrl))
			return;

		try
		{
			await Web.SendAsync(HttpMethod.Get, _offloadUrl);
		}
		catch (Exception e)
		{
			throw new WebException($"Error with offload server. {e.Message}");
		}
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
	
	public async Task RemoteBuildReceived(string buildId, string buildPath, byte[] data)
	{
		if (!_buildIds.Contains(buildId))
			throw new Exception($"Build ID not expected: {buildId}");

		await FilePacker.UnpackRawAsync($"{buildPath}.zip", data, buildPath);
		_buildIds.Remove(buildId);
	}

	/// <summary>
	/// Returns once buildIds count is 0
	/// </summary>
	private async Task WaitBuildIds()
	{
		var cachedCount = -1;
		
		while (_buildIds.Count > 0)
		{
			// to limit the amount of log spamming just log when count changes
			if (_buildIds.Count != cachedCount)
			{
				Logger.Log($"Remaining buildIds: ({_buildIds.Count}) {string.Join(", ", _buildIds)}");
				cachedCount = _buildIds.Count;
			}
			
			await Task.Delay(3000);
		}
	}

	#endregion

}
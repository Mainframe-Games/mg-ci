using System.Net;
using System.Text;
using Builder;
using Builds;
using Deployment.Configs;
using SharedLib;
using SharedLib.ChangeLogBuilders;
using SharedLib.Webhooks;

namespace Deployment;

public class OffloadServerPacket
{
	public string? WorkspaceName { get; set; }
	public BuildVersions? BuildVersion { get; set; }
	public int ChangesetId { get; set; }
	public bool CleanBuild { get; set; }
	public ParallelBuildConfig? ParallelBuild { get; set; }
	
	/// <summary>
	/// BuildId, Config
	/// </summary>
	public Dictionary<string, TargetConfig> Builds { get; set; }
}

public class BuildPipeline
{
	public static BuildPipeline? Current { get; private set; }

	public delegate void OffloadBuildReqPacket(OffloadServerPacket packet);
	public delegate string? ExtraHookLogs();
	public delegate Task DeployDelegate(DeployContainerConfig deploy, string buildVersionTitle);
	
	public event OffloadBuildReqPacket OffloadBuildNeeded;
	public event ExtraHookLogs GetExtraHookLogs;
	public event DeployDelegate DeployEvent;

	private readonly Args _args;
	private readonly string? _offloadUrl;
	private readonly List<UnityTarget> _offloadTargets;
	private BuildConfig _config;

	public Workspace Workspace { get; }
	private DateTime StartTime { get; set; }
	private string TimeSinceStart => $"{DateTime.Now - StartTime:hh\\:mm\\:ss}";
	private string BuildVersionTitle => $"Build Version: {_buildVersion.BundleVersion}";

	/// <summary>
	/// The change set id that was current when build started
	/// </summary>
	private int _currentChangeSetId;
	private string _currentGuid;
	private int _previousChangeSetId;
	private BuildVersions _buildVersion;

	/// <summary>
	/// build ids we are waiting for offload server
	/// </summary>
	private readonly List<string> _buildIds = new();

	public BuildPipeline(Workspace workspace, string[]? args, string? offloadUrl, List<UnityTarget>? offloadTargets)
	{
		Workspace = workspace;
		_args = new Args(args);
		_offloadUrl = offloadUrl;
		_offloadTargets = offloadTargets ?? new List<UnityTarget>();
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

		Workspace.GetCurrent(out _currentChangeSetId, out _currentGuid);
		_previousChangeSetId = Workspace.GetPreviousChangeSetId();
		Logger.Log($"[CHANGESET] cs:{_previousChangeSetId} \u2192 cs:{_currentChangeSetId}, guid:{_currentGuid}");
		
		_config = BuildConfig.GetConfig(Workspace.Directory); // refresh config

		// pre build runner
		var preBuild = new PreBuild(Workspace);
		preBuild.Run(_config.PreBuild);
		
		// write to disk
		var writer = new ProjectSettingsWriter(Workspace.ProjectSettingsPath);
		writer.ReplaceVersions(preBuild.BuildVersion);
		
		_buildVersion = preBuild.BuildVersion;
		await Task.CompletedTask;
	}
	
	private async Task Build()
	{
		if (_args.IsFlag("-nobuild"))
			return;
		
		if (_config?.Builds == null)
			throw new NullReferenceException();
		
		if (_config.ParallelBuild != null)
			await ClonesManager.CloneProject(Workspace.Directory,
				_config.ParallelBuild.Links,
				_config.ParallelBuild.Copies,
				_config.Builds.Where(x => !IsOffload(x)));
		
		Logger.Log("Build process started...");
		var buildStartTime = DateTime.Now;
		
		var tasks = new List<Task>();

		OffloadServerPacket? offloadBuilds = null;

		foreach (var build in _config.Builds)
		{
			var unity = new LocalUnityBuild(Workspace.UnityVersion);
			
			if (unity == null || _config?.Builds == null)
				throw new NullReferenceException();
			
			// offload build
			if (IsOffload(build))
			{
				offloadBuilds ??= new OffloadServerPacket
				{
					WorkspaceName = Workspace.Name,
					ChangesetId = _currentChangeSetId,
					BuildVersion = _buildVersion,
					CleanBuild = _args.IsFlag("-cleanbuild"),
					ParallelBuild = _config.ParallelBuild,
					Builds = new Dictionary<string, TargetConfig>()
				};
				
				var buildId = Guid.NewGuid().ToString();
				offloadBuilds.Builds[buildId] = build;
				_buildIds.Add(buildId);
			}
			// local build
			else
			{
				if (_config.ParallelBuild != null)
				{
					build.BuildPath = Path.Combine(Workspace.Directory, build.BuildPath);
					var targetPath = ClonesManager.GetTargetPath(Workspace.Directory, build);
					var task = Task.Run(() => unity.Build(targetPath, build));
					tasks.Add(task);
				}
				else
				{
					unity.Build(Workspace.Directory, build);
				}
			}
		}

		// send offload builds
		if (offloadBuilds != null)
			OffloadBuildNeeded?.Invoke(offloadBuilds);

		if (tasks.Count > 0)
			tasks.WaitForAll();
		
		await WaitBuildIds();
		Logger.LogTimeStamp("Build time", buildStartTime);
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
		return !string.IsNullOrEmpty(_offloadUrl) && _offloadTargets.Contains(target.Target ?? UnityTarget.None);
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
		Workspace.CommitNewVersionNumber(_currentChangeSetId, $"{BuildVersionTitle} | cs: {_currentChangeSetId} | guid: {_currentGuid}");
		
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
				hookMessage.AppendLine($"ChangesetId: {_currentChangeSetId}");
				hookMessage.AppendLine($"GUID: {_currentGuid}");
				hookMessage.AppendLine(discord.ToString());
				Discord.PostMessage(hook.Url, hookMessage.ToString(), hook.Title, BuildVersionTitle, Discord.Colour.GREEN);
			}
			else if (hook.IsSlack())
			{
				var hookMessage = new StringBuilder();
				hookMessage.AppendLine($"{hook.Title} | {BuildVersionTitle}");
				hookMessage.AppendLine($"Total Time: {TimeSinceStart}");
				hookMessage.AppendLine($"ChangesetId: {_currentChangeSetId}");
				hookMessage.AppendLine($"GUID: {_currentGuid}");
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
		var errorMessage = $"{e.GetType()}: {e.Message}";
		
		foreach (var hook in _config.Hooks)
		{
			if (!hook.IsErrorChannel)
				continue;
			
			hookMessage.Clear();
			
			if (hook.IsDiscord())
			{
				hookMessage.AppendLine(hook.Title);
				hookMessage.AppendLine(errorMessage);
				Discord.PostMessage(hook.Url, hookMessage.ToString(), hook.Title, BuildVersionTitle, Discord.Colour.RED);
			}
			else if (hook.IsSlack())
			{
				hookMessage.AppendLine(hook.Title);
				hookMessage.AppendLine(errorMessage);
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
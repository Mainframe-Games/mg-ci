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
	public string? Branch { get; set; }
	public bool CleanBuild { get; set; }
	public ParallelBuildConfig? ParallelBuild { get; set; }
	public ulong PipelineId { get; set; }
	
	/// <summary>
	/// BuildId (GUID), Asset Name <see cref="BuildSettingsAsset"/>
	/// </summary>
	public Dictionary<string, string> Builds { get; set; }

}

public class BuildPipeline
{
	/// <summary>
	/// Key to use to tag version bump commit and find previous build commit
	/// </summary>
	private const string BUILD_VERSION = "Build Version:";
	
	public delegate void OffloadBuildReqPacket(OffloadServerPacket packet);
	public delegate string? ExtraHookLogs(BuildPipeline pipeline);
	public delegate Task DeployDelegate(BuildPipeline pipeline);
	
	public event OffloadBuildReqPacket OffloadBuildNeeded;
	public event ExtraHookLogs GetExtraHookLogs;
	public event DeployDelegate DeployEvent;
	
	public readonly ulong Id;
	private readonly string? _offloadUrl;
	private readonly bool _offloadParallel;
	private readonly List<UnityTarget> _offloadTargets;

	public Workspace Workspace { get; }
	public Args Args { get; }
	public BuildConfig Config { get; private set; }
	private DateTime StartTime { get; set; }
	private string TimeSinceStart => $"{DateTime.Now - StartTime:hh\\:mm\\:ss}";
	public string BuildVersionTitle => $"{BUILD_VERSION} {_buildVersion?.BundleVersion}";

	/// <summary>
	/// The change set id that was current when build started
	/// </summary>
	private readonly int _currentChangeSetId;
	private readonly string _currentGuid;
	private BuildVersions _buildVersion;

	public string[] ChangeLog { get; }
	
	/// <summary>
	/// build ids we are waiting for offload server
	/// </summary>
	private readonly List<string> _buildIds = new();

	public BuildPipeline(ulong id, Workspace workspace, Args args, string? offloadUrl, bool offloadParallel, List<UnityTarget>? offloadTargets)
	{
		Id = id;
		Workspace = workspace;
		Args = args;
		
		_offloadUrl = offloadUrl;
		_offloadParallel = offloadParallel;
		_offloadTargets = offloadTargets ?? new List<UnityTarget>();
		
		Environment.CurrentDirectory = workspace.Directory;
		
		// clear and update workspace 
		if (Args.IsFlag("-cleanbuild"))
			Workspace.CleanBuild();
		
		Workspace.Clear();
		Args.TryGetArg("-changesetid", out var idStr, "-1");
		Workspace.Update(int.Parse(idStr));
		Workspace.GetCurrent(out _currentChangeSetId, out _currentGuid);
		
		var prevChangeSetId = Workspace.GetPreviousChangeSetId(BUILD_VERSION);
		Logger.Log($"[CHANGESET] cs:{prevChangeSetId} \u2192 cs:{_currentChangeSetId}, guid:{_currentGuid}");

		Config = BuildConfig.GetConfig(Workspace.Directory);
		ChangeLog = Workspace.GetChangeLog(_currentChangeSetId, prevChangeSetId);
	}

	#region Build Steps

	public async Task<bool> RunAsync()
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
			return true;
		}
		catch (Exception e)
		{
			Logger.Log(e);
			SendErrorHook(e);
		}
		
		return false;
	}

	private async Task Prebuild()
	{
		if (Args.IsFlag("-noprebuild"))
			return;

		Logger.Log("PreBuild process started...");

		// pre build runner
		var preBuild = new PreBuild(Workspace);
		preBuild.Run(Config.PreBuild);
		
		// write new versions to disk
		Workspace.ProjectSettings.ReplaceVersions(preBuild.BuildVersion);
		
		_buildVersion = preBuild.BuildVersion;
		await Task.CompletedTask;
	}

	private IEnumerable<BuildSettingsAsset> GetTargetsToBuild()
	{
		var all = Workspace.GetBuildTargets();
		return all.Where(x => x.GetValue<int>("Ignore") == 0);

		// if (Args.TryGetArg("-targets", out string targetsRaw))
		// {
		// 	var targets = targetsRaw.Split(',');
		// 	return all.Where(x => targets.Contains(x.Name));
		// }
	}
	
	private async Task Build()
	{
		if (Args.IsFlag("-nobuild"))
			return;
		
		// TODO: come back to this if we need parallel builds again.
		// I can't image we'd need to, it turned out to be slower and complicated things more
		// if (Config.ParallelBuild != null)
		// 	await ClonesManager.CloneProject(Workspace.Directory,
		// 		Config.ParallelBuild.Links,
		// 		Config.ParallelBuild.Copies,
		// 		Config.Builds.Where(x => !IsOffload(x)));
		
		Logger.Log("Build process started...");
		var buildStartTime = DateTime.Now;
		
		var parallelTasks = new List<Task>(); // for parallel builds
		var localBuilds = new List<BuildSettingsAsset>(); // for sequential builds

		OffloadServerPacket? offloadBuilds = null;
		
		var builds = GetTargetsToBuild().ToArray();
		
		foreach (var build in builds)
		{
			// offload build
			if (IsOffload(build.Target))
			{
				offloadBuilds ??= new OffloadServerPacket
				{
					WorkspaceName = Workspace.Name,
					ChangesetId = _currentChangeSetId,
					BuildVersion = _buildVersion,
					PipelineId = Id,
					CleanBuild = Args.IsFlag("-cleanbuild"),
					Branch = Workspace.Branch,
					ParallelBuild = _offloadParallel ? Config.ParallelBuild : null,
					Builds = new()
				};
				
				var buildId = Guid.NewGuid().ToString();
				offloadBuilds.Builds[buildId] = build.Name;
				_buildIds.Add(buildId);
			}
			// local build
			else
			{
				// TODO: come back to this if we need parallel builds again.
				// if (Config.ParallelBuild != null)
				// {
				// 	build.BuildPath = Path.Combine(Workspace.Directory, build.BuildPath);
				// 	var targetPath = ClonesManager.GetTargetPath(Workspace.Directory, build);
				// 	var unity = new LocalUnityBuild(Workspace);
				// 	var task = Task.Run(() => unity.Build(targetPath, build));
				// 	parallelTasks.Add(task);
				// }
				// else
				{
					localBuilds.Add(build);
				}
			}
		}

		// send offload builds first
		if (offloadBuilds != null)
			OffloadBuildNeeded?.Invoke(offloadBuilds);

		// then wait for local builds
		if (parallelTasks.Count > 0)
		{
			parallelTasks.WaitForAll();
		}
		else
		{
			// local sequential builds
			// this needs to be after off loads event is invoked otherwise
			// we'll just be idling doing nothing while offload builds could be running
			foreach (var localBuild in localBuilds)
			{
				var unity = new LocalUnityBuild(Workspace);
				unity.Build(localBuild);
			}
		}
		
		// wait for offload builds to complete
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
	private bool IsOffload(UnityTarget target)
	{
		return !string.IsNullOrEmpty(_offloadUrl) && _offloadTargets.Contains(target);
	}

	private async Task DeployAsync()
	{
		if (Args.IsFlag("-nodeploy"))
			return;
		
		if (Config.Deploy == null)
			return;
		
		await DeployEvent.Invoke(this);
	}

	private async Task PostBuild()
	{
		if (Args.IsFlag("-nopostbuild"))
			return;

		Logger.Log("PostBuild process started...");
		
		// committing new version must be done after collecting changeLogs as the prev changesetid will be updated
		Workspace.CommitNewVersionNumber($"{BuildVersionTitle} | cs: {_currentChangeSetId} | guid: {_currentGuid}");
		
		if (Config.Hooks == null || Args.IsFlag("-nohooks"))
			return;

		// optional message from clanforge
		var clanforgeMessage = Config.Deploy?.Clanforge != null
			? GetExtraHookLogs?.Invoke(this)
			: null;

		foreach (var hook in Config.Hooks)
		{
			if (hook.IsErrorChannel == 1)
				continue;
			
			var hookMessage = new StringBuilder();

			if (hook.IsDiscord())
			{
				var discord = new ChangeLogBuilderDiscord();
				discord.BuildLog(ChangeLog);
				
				hookMessage.AppendLine($"Total Time: {TimeSinceStart}");
				hookMessage.AppendLine(clanforgeMessage);
				hookMessage.AppendLine($"cs: {_currentChangeSetId}");
				hookMessage.AppendLine($"guid: {_currentGuid}");
				hookMessage.AppendLine(discord.ToString());
				Discord.PostMessage(hook.Url, hookMessage.ToString(), hook.Title, BuildVersionTitle, Discord.Colour.GREEN);
			}
			else if (hook.IsSlack())
			{
				hookMessage.AppendLine($"*{hook.Title}*");
				hookMessage.AppendLine(BuildVersionTitle);
				hookMessage.AppendLine($"Total Time: {TimeSinceStart}");
				hookMessage.AppendLine(clanforgeMessage);
				hookMessage.AppendLine($"cs: {_currentChangeSetId}");
				hookMessage.AppendLine($"guid: {_currentGuid}");
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
		if(Config.Hooks == null)
			return;
		
		var hookMessage = new StringBuilder();
		var errorMessage = $"{e.GetType()}: {e.Message}";
		
		foreach (var hook in Config.Hooks)
		{
			if (hook.IsErrorChannel == 0)
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
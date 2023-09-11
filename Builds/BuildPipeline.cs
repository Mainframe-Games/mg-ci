using System.Net;
using System.Text;
using Builds;
using Deployment.Configs;
using SharedLib;
using SharedLib.Build;
using SharedLib.BuildToDiscord;
using SharedLib.ChangeLogBuilders;
using SharedLib.Webhooks;

namespace Deployment;

public class BuildPipeline
{
	/// <summary>
	/// Key to use to tag version bump commit and find previous build commit
	/// </summary>
	private const string BUILD_VERSION = "Build Version:";
	
	public delegate string? ExtraHookLogs(BuildPipeline pipeline);
	public delegate Task<bool> DeployDelegate(BuildPipeline pipeline);
	
	public event ExtraHookLogs GetExtraHookLogs;
	public event DeployDelegate DeployEvent;
	
	public readonly ulong Id;
	private readonly IOffloadable? _offloadable;

	/// <summary>
	/// The change set id that was current when build started
	/// </summary>
	private readonly int _currentChangeSetId;
	private readonly string _currentGuid;
	
	/// <summary>
	/// build ids we are waiting for offload server
	/// </summary>
	private readonly List<BuildResult> _buildResults = new();
	
	public Workspace Workspace { get; }
	public Args Args { get; }
	public BuildConfig Config { get; }
	public BuildVersions? BuildVersions { get; private set; }
	public string[] ChangeLog { get; }
	public PipelineReport Report { get; }
	private DateTime StartTime { get; set; }
	private string TimeSinceStart => $"{(DateTime.Now - StartTime).ToHourMinSecString()}";

	public BuildPipeline(ulong id, Workspace workspace, Args args, IOffloadable? offloadable)
	{
		Id = id;
		Workspace = workspace;
		Args = args;

		_offloadable = offloadable;

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
		
		var buildTargetNames = Workspace.GetBuildTargets().Select(x => x.Name).ToArray();
		Report = new PipelineReport(buildTargetNames);
	}

	#region Build Steps

	public async Task<bool> RunAsync()
	{
		try
		{
			StartTime = DateTime.Now;

			// Prebuild
			Report.Update(PipelineStage.PreBuild, BuildTaskStatus.Pending);
			if (!await PingOffloadServer()) return false;
			await Prebuild();
			Report.Update(PipelineStage.PreBuild, BuildTaskStatus.Succeed);

			// Build Targets
			Report.Update(PipelineStage.Build, BuildTaskStatus.Pending);
			await Build();
			Report.Update(PipelineStage.Build, BuildTaskStatus.Succeed);

			// deploy
			Report.Update(PipelineStage.Deploy, BuildTaskStatus.Pending);
			if (!await DeployAsync())
			{
				Report.Update(PipelineStage.Deploy, BuildTaskStatus.Failed);
				return false;
			}

			Report.Update(PipelineStage.Deploy, BuildTaskStatus.Succeed);

			// post build
			Report.Update(PipelineStage.PostBuild, BuildTaskStatus.Pending);
			PostBuild();
			Report.Update(PipelineStage.PostBuild, BuildTaskStatus.Succeed);

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
		var preBuild = new PreBuild(Workspace, Args);
		preBuild.Run(Config.PreBuild);
		BuildVersions = preBuild.BuildVersions;
		
		// write new versions to disk
		Workspace.ProjectSettings.ReplaceVersions(preBuild.BuildVersions);
		Workspace.SaveBuildVersion(preBuild.BuildVersions.FullVersion);
		Workspace.CommitNewVersionNumber($"{BUILD_VERSION}: {preBuild.BuildVersions.FullVersion} | cs: {_currentChangeSetId} | guid: {_currentGuid}");
		
		await Task.CompletedTask;
	}

	private async Task Build()
	{
		if (Args.IsFlag("-nobuild"))
			return;
		
		Logger.Log("Build process started...");
		var buildStartTime = DateTime.Now;
        
		var localBuilds = new List<BuildSettingsAsset>(); // for sequential builds

		var builds = Workspace.GetBuildTargets().ToArray();
		Logger.Log($"Building targets... {string.Join(", ", builds.Select(x => x.Name))}");
		
		foreach (var build in builds)
		{
			if (_offloadable?.IsOffload(build, BuildVersions, _currentChangeSetId, Args.IsFlag("-cleanbuild")) is true)
			{
				Report.UpdateBuildTarget(build.Name, default);
			}
			// local build
			else
			{
				localBuilds.Add(build);
			}
		}

		// send offload builds first
		if (_offloadable is not null)
			await _offloadable.SendAsync();

		// local sequential builds
		// this needs to be after off loads event is invoked otherwise
		// we'll just be idling doing nothing while offload builds could be running
		foreach (var localBuild in localBuilds)
		{
			var unity = new LocalUnityBuild(Workspace);
			Report.UpdateBuildTarget(localBuild.Name, BuildTaskStatus.Pending);
			var buildResult = unity.Build(localBuild);
			_buildResults.Add(buildResult);
			Report.UpdateBuildTarget(localBuild.Name, buildResult.IsErrors ? BuildTaskStatus.Failed : BuildTaskStatus.Succeed);
		}
		
		// wait for offload builds to complete
		if (_offloadable is not null)
			await _offloadable.WaitBuildIdsAsync();
		
		Logger.LogTimeStamp("Build time", buildStartTime);
	}

	private async Task<bool> DeployAsync()
	{
		if (Args.IsFlag("-nodeploy"))
			return true;
		
		if (Config.Deploy == null)
			return true;
		
		return await DeployEvent.Invoke(this);
	}

	private void PostBuild()
	{
		if (Args.IsFlag("-nopostbuild"))
			return;

		Logger.Log("PostBuild process started...");

		if (Report.IsFailed)
		{
			Report.Complete(BuildTaskStatus.Failed, "Pipeline Failed", "Nothing else to add");
			return;
		}
		
		if (Config.Hooks == null || Args.IsFlag("-nohooks"))
			return;

		// optional message from clanforge
		var extraLogMessage = GetExtraHookLogs.Invoke(this);
		
		// build changeLog
		var hookMessage = new StringBuilder();
		hookMessage.AppendLine($"**ChangeSetId:** {_currentChangeSetId}");
		hookMessage.AppendLine($"**ChangeSetGUID:** {_currentGuid}");
		hookMessage.AppendLine("");

		hookMessage.AppendLine($"**Targets:** Total Time {TimeSinceStart}");
		foreach (var buildResult in _buildResults)
			hookMessage.AppendLine($"- {buildResult}");
		hookMessage.AppendLine("");

		if (!string.IsNullOrEmpty(extraLogMessage))
		{
			hookMessage.AppendLine(extraLogMessage);
			hookMessage.AppendLine("");
		}
		
		hookMessage.AppendLine("**Change Log:**");
		var discord = new ChangeLogBuilderDiscord();
		discord.BuildLog(ChangeLog);
		hookMessage.AppendLine(discord.ToString());

		var title = $"{Workspace.Meta?.ProjectName ?? Workspace.Name} | {BuildVersions?.FullVersion}";
		
		// send hooks
		foreach (var hook in Config.Hooks)
		{
			if (hook.IsErrorChannel is true)
				continue;
			
			if (hook.IsDiscord())
			{
				var embed = new Discord.Embed
				{
					Url = Workspace.Meta?.Url,
					ThumbnailUrl = Workspace.Meta?.ThumbnailUrl,
					Title = title,
					Description = hookMessage.ToString(),
					Username = hook.Title,
					Colour = Discord.Colour.GREEN
				};
				Discord.PostMessage(hook.Url, embed);
			}
			else if (hook.IsSlack())
			{
				var slackMessage = $"*{hook.Title}*\n{title}\n{hookMessage}";
				Slack.PostMessage(hook.Url, slackMessage);
			}
		}
		
		Report.Complete(BuildTaskStatus.Succeed, title, hookMessage.ToString());
		Workspace.Clear();
	}
	
	#endregion

	#region Helper Methods

	/// <summary>
	/// Pings Offload server to check if its awake
	/// </summary>
	/// <exception cref="WebException"></exception>
	private async Task<bool> PingOffloadServer()
	{
		// ignore if no offload server is needed
		if (_offloadable is null)
			return true;
		
		var res = await Web.SendAsync(HttpMethod.Get, _offloadable.Url);
		
		if (res.StatusCode is HttpStatusCode.OK)
			return true;
		
		Logger.Log($"Offload server failure: {res.ToString()}");
		Report.Update(PipelineStage.PreBuild, BuildTaskStatus.Failed);
		return false;
	}

	public void SendErrorHook(Exception e)
	{
		var errorMessage = $"{e.GetType()}: {e.Message}";
		Report.Complete(BuildTaskStatus.Failed, "Pipeline Failed", errorMessage);
		
		if (Config.Hooks == null)
			return;
		
		var title = $"{Workspace.Meta?.ProjectName ?? Workspace.Name} | {BuildVersions?.FullVersion}";
		
		var hookMessage = new StringBuilder();
		
		foreach (var hook in Config.Hooks)
		{
			if (hook.IsErrorChannel is not true)
				continue;
			
			hookMessage.Clear();
			
			if (hook.IsDiscord())
			{
				hookMessage.AppendLine(hook.Title);
				hookMessage.AppendLine(errorMessage);
				Discord.PostMessage(hook.Url, hookMessage.ToString(), hook.Title, title, Discord.Colour.RED);
			}
			else if (hook.IsSlack())
			{
				hookMessage.AppendLine(hook.Title);
				hookMessage.AppendLine(errorMessage);
				Slack.PostMessage(hook.Url, hookMessage.ToString());
			}
		}
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="buildGuid"></param>
	/// <param name="buildName"></param>
	/// <param name="buildTaskStatus"></param>
	/// <param name="buildResult">Can be null for pending status requests</param>
	/// <exception cref="Exception"></exception>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public void SetOffloadBuildStatus(string buildGuid, string buildName, BuildTaskStatus buildTaskStatus, BuildResult? buildResult = null)
	{
		if (_offloadable is null)
			return;
		
		if (!_offloadable.PendingIds.Contains(buildGuid))
			throw new Exception($"{nameof(buildGuid)} not expected: {buildGuid}");

		Logger.Log($"Offload Status: {buildName}, status: {buildTaskStatus}, result: {buildResult}");
		Report.UpdateBuildTarget(buildName, buildTaskStatus);

		switch (buildTaskStatus)
		{
			case BuildTaskStatus.Queued:
			case BuildTaskStatus.Pending:
				break;
			
			case BuildTaskStatus.Succeed:
				_buildResults.Add(buildResult);
				_offloadable.PendingIds.Remove(buildGuid);
				break;
			
			case BuildTaskStatus.Failed:
				_buildResults.Add(buildResult);
				_offloadable.PendingIds.Remove(buildGuid);
				Logger.Log($"{buildName} Errors: {buildResult?.Errors}");
				break;
			
			default:
				throw new ArgumentOutOfRangeException(nameof(buildTaskStatus), buildTaskStatus, null);
		}
	}

	#endregion
}
﻿using System.Net;
using System.Reflection;
using System.Text;
using Deployment.RemoteBuild;
using SharedLib;
using SharedLib.BuildToDiscord;
using SharedLib.Server;

namespace Server.RemoteBuild;

public class RemoteBuildWorkspaceRequest : IRemoteControllable
{
	public string? WorkspaceName { get; set; }
	public string? Args { get; set; }
	
	public string? DiscordAddress { get; set; }
	public ulong MessageId { get; set; }
	
	public ServerResponse Process()
	{
		var argsArray = Args?.Split(' ');
		var args = new Args(argsArray);
		args.TryGetArg("-branch", out var branch, "main");

		var mapping = new WorkspaceMapping();
		var workspaceName = mapping.GetRemapping(WorkspaceName);
		var workspace = Workspace.GetWorkspaceFromName(workspaceName);
		
		if (workspace == null)
			return new ServerResponse(HttpStatusCode.BadRequest, $"Given namespace is not valid: {WorkspaceName}");
		
		Logger.Log($"Chosen workspace: {workspace}");
		
		workspace.Clear();
		workspace.Update();
		workspace.SwitchBranch(branch);

		var pipeline = App.CreateBuildPipeline(workspace, args);
		pipeline.Report.OnReportUpdated += OnReportUpdated;
		OnReportUpdated(pipeline.Report);

		if (pipeline.ChangeLog.Length == 0)
			return new ServerResponse(HttpStatusCode.NotAcceptable, "No changes to build");
		
		App.RunBuildPipe(pipeline).FireAndForget();
		workspace.GetCurrent(out var changeSetId, out var guid);

		var data = new BuildPipelineResponse
		{
			ServerVersion = App.Version,
			PipelineId = pipeline.Id,
			Workspace = workspace.Name,
			Targets = string.Join(", ", workspace.GetBuildTargets().Select(x => x.Name)),
			Args = Args,
			UnityVersion = workspace.UnityVersion,
			ChangesetId = changeSetId,
			ChangesetGuid = guid,
			Branch = branch,
			ChangesetCount = pipeline.ChangeLog.Length,
		};
		return new ServerResponse(HttpStatusCode.OK, data.ToString());
	}

	private async void OnReportUpdated(PipelineReport report)
	{
		if (string.IsNullOrEmpty(DiscordAddress))
			return;

		var pipelineUpdate = new PipelineUpdateMessage
		{
			MessageId = MessageId,
			Report = report,
		};

		await Web.SendAsync(HttpMethod.Post, DiscordAddress, body: pipelineUpdate);
	}
}

public class BuildPipelineResponse
{
	public string? ServerVersion { get; set; }
	public ulong? PipelineId { get; set; }
	public string? Workspace { get; set; }
	public string? Targets { get; set; }
	public string? Args { get; set; }
	public string? Branch { get; set; }
	public int? ChangesetId { get; set; }
	public string? ChangesetGuid { get; set; }
	public string? UnityVersion { get; set; }
	public int? ChangesetCount { get; set; }

	public override string ToString()
	{
		var str = new StringBuilder();

		var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

		foreach (var propertyInfo in properties)
		{
			var value = propertyInfo.GetValue(this);
			
			if (value is null)
				continue;
			
			str.AppendLine($"**{propertyInfo.Name}**: {value}");
		}
		
		return str.ToString();
	}
}
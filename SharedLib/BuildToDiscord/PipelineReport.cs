﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SharedLib.BuildToDiscord;

public class PipelineReport
{
	public event Action<PipelineReport> OnReportUpdated;
	
	public BuildTaskStatus PreBuild { get; set; }
	public BuildTaskStatus Build { get; set; }
	public BuildTaskStatus Deploy { get; set; }
	public BuildTaskStatus PostBuild { get; set; }
	public Dictionary<string, BuildTaskStatus> BuildTargets { get; set; } = new();
	
	public string? CompleteTitle { get; set; }
	/// <summary>
	/// Generally the change log, but can be error messages as well
	/// </summary>
	public string? CompleteMessage { get; set; }

	[JsonIgnore] public bool IsPending => GetAllStatuses().Any(x => x is BuildTaskStatus.Pending);
	[JsonIgnore] public bool IsFailed => GetAllStatuses().Any(x => x is BuildTaskStatus.Failed);
	[JsonIgnore] public bool IsSuccessful => GetAllStatuses().All(x => x is BuildTaskStatus.Succeed);

	public PipelineReport() {}
	
	public PipelineReport(IEnumerable<string> buildTargetNames)
	{
		foreach (var buildTargetName in buildTargetNames)
			BuildTargets.Add(buildTargetName, default);
	}

	private IEnumerable<BuildTaskStatus> GetAllStatuses()
	{
		yield return PreBuild;
		yield return Build;
		yield return Deploy;
		yield return PostBuild;

		foreach (var target in BuildTargets)
			yield return target.Value;
	}

	public void Update(PipelineStage stage, BuildTaskStatus status)
	{
		switch (stage)
		{
			case PipelineStage.PreBuild:
				PreBuild = status;
				break;
			case PipelineStage.Build:
				Build = status;
				break;
			case PipelineStage.Deploy:
				Deploy = status;
				break;
			case PipelineStage.PostBuild:
				PostBuild = status;
				break;
			
			default:
				throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
		}
		
		OnReportUpdated?.Invoke(this);
	}

	public void UpdateBuildTarget(string buildTargetName, BuildTaskStatus status)
	{
		if (BuildTargets.ContainsKey(buildTargetName))
			BuildTargets[buildTargetName] = status;
		
		OnReportUpdated?.Invoke(this);
	}
	
	public void Complete(BuildTaskStatus status, string title, string message)
	{
		PostBuild = status;
		CompleteTitle = title;
		CompleteMessage = message;
		
		OnReportUpdated?.Invoke(this);
	}

	public string BuildDescription()
	{
		var stringBuilder = new StringBuilder();

		stringBuilder.AppendLine("**Pipeline Steps**");
		stringBuilder.AppendLine(GetDiscordLine(nameof(PreBuild), PreBuild));
		stringBuilder.AppendLine(GetDiscordLine(nameof(Build), Build));
		stringBuilder.AppendLine(GetDiscordLine(nameof(Deploy), Deploy));
		stringBuilder.AppendLine(GetDiscordLine(nameof(PostBuild), PostBuild));

		return stringBuilder.ToString();
	}

	private static string GetDiscordLine(string name, BuildTaskStatus status)
	{
		var emote = Emotes.GetStatus(status);
		return $"{emote} **{name}** *{status}*";
	}

	public IEnumerable<KeyValuePair<string, string>> BuildTargetFields()
	{
		return BuildTargets.Select(step => 
			new KeyValuePair<string, string>(step.Key, $"{Emotes.GetStatus(step.Value)} {step.Value}"));
	}
}
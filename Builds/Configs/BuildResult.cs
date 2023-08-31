using System.Text.Json.Serialization;
using SharedLib;

namespace Deployment.Configs;

public class BuildResult
{
	public string? BuildName { get; set; }
	public TimeSpan BuildTime { get; set; }
	public ulong BuildSize { get; set; }
	public string? Errors { get; set; }
	[JsonIgnore] public bool IsErrors => !string.IsNullOrEmpty(Errors);

	public override string ToString()
	{
		return $"**{BuildName}**: {BuildTime.ToHourMinSecString()} ({BuildSize.ToByteSizeString()})";
	}
}
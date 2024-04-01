using System.Text.Json.Serialization;
using SharedLib;
using SharedLib.Server;

namespace Deployment.Configs;

public class BuildResult
{
    public string? BuildName { get; set; }
    public TimeSpan BuildTime { get; set; }
    public ulong BuildSize { get; set; }
    public ErrorResponse? Errors { get; set; }

    [JsonIgnore]
    public bool IsErrors => Errors is not null;

    public override string ToString()
    {
        return $"**{BuildName}**: {BuildTime.ToHourMinSecString()} ({BuildSize.ToByteSizeString()})";
    }
}

using SharedLib;

namespace Deployment.Configs;

public class BuildResult
{
	public string? BuildName { get; set; }
	public TimeSpan BuildTime { get; set; }
	public ulong BuildSize { get; set; }

	public override string ToString()
	{
		return $"{BuildName} {BuildTime} ({BuildSize.ToByteSizeString()})";
	}
}
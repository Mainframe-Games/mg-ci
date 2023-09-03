using SharedLib.BuildToDiscord;

namespace SharedLib;

public static class Emotes
{
	public static string GetStatus(BuildTaskStatus status)
	{
		return status switch
		{
			BuildTaskStatus.Queued => "💤",
			BuildTaskStatus.Pending => "⏳",
			BuildTaskStatus.Succeed => "✅",
			BuildTaskStatus.Failed => "❌",
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
		};
	}
}
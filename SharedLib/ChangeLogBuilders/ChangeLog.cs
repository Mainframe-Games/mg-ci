namespace SharedLib.ChangeLogBuilders;

/// <summary>
/// Used for making massive patch notes from specific changeset.
/// Good for Steam announcements
/// </summary>
public class ChangeLog
{
	public static void LogToFileSteam()
	{
		Environment.CurrentDirectory = "/Users/broganking/Unity/Out of Sight";
		
		var changeLog = GetLogs(1374);
		var steam = new ChangeLogBuilderSteam();
		var commits = changeLog.Split(Environment.NewLine);
		if (steam.BuildLog(commits))
		{
			var path = Path.Combine("/Users/broganking/Downloads", "SteamChangeLog.txt");
			File.WriteAllText(path, steam.ToString());
		}
		else
		{
			Logger.Log("Failed to write steam log");
		}
	}
	
	/// <summary>
	/// Returns raw change set logs from plastic
	/// </summary>
	/// <param name="fromChangeSetId"></param>
	/// <returns></returns>
	public static string GetLogs(int fromChangeSetId)
	{
		return Cmd.Run("cm", $"log --from=cs:{fromChangeSetId} --csformat=\"{{comment}}\"").output;
	}
}
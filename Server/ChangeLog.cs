using Deployment.Configs;
namespace SharedLib.ChangeLogBuilders;

/// <summary>
/// Used for making massive patch notes from specific changeset.
/// Good for Steam announcements
/// </summary>
public class ChangeLog
{
	public static void LogToFileSteam()
	{
		var workspace = Workspace.AskWorkspace();
		Environment.CurrentDirectory = workspace.Directory;

		Console.Write("from: ");
		var csFrom = Console.ReadLine() ?? "0";
		Console.Write("to: ");
		var csTo = Console.ReadLine() ?? "0";
		
		var changeLog = Workspace.GetChangeLog(int.Parse(csTo), int.Parse(csFrom));

		var config = BuildConfig.GetConfig(workspace.Directory);
		var url = config.Hooks?.FirstOrDefault(x => x.IsDiscord() && !x.IsErrorChannel)?.Url;

		if (string.IsNullOrEmpty(url))
			throw new NullReferenceException();
		
		var discord = new ChangeLogBuilderSteam();
		discord.BuildLog(changeLog);
		var output = discord.ToString();

		var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		File.WriteAllText($"{desktop}/changelog.txt", output);
		// Discord.PostMessage(url, discord.ToString(), "Changelog print out", $"cs: {csFrom} - {csTo}");
	}
}
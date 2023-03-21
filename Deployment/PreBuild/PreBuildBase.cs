using SharedLib;

namespace Deployment.PreBuild;

public enum PreBuildType
{
	/// <summary>
	/// No prebuild
	/// </summary>
	None,
	
	/// <summary>
	/// MAJOR - Single number increments
	/// </summary>
	Major,
	
	/// <summary>
	/// MAJOR.MINOR - Minor version increments. MAJOR must be done manually.
	/// </summary>
	Major_Minor,
	
	/// <summary>
	/// MAJOR.CHANGE_SET_ID - Major version increments. ChangeSetId from HEAD on Plastic
	/// </summary>
	Major_ChangeSetId
}

public abstract class PreBuildBase
{
	private const string PROJECT_SETTINGS = "ProjectSettings/ProjectSettings.asset";
	private const string PREV_CHANGESET_ID_PATH = "BuildScripts/previous-changesetId.txt";
	private const string STEAM_DIR_PATH = "BuildScripts/Steam";
	
	public int CurrentChangeSetId { get; protected set; }
	public static int PreviousChangeSetId => GetPreviousChangeSetId();
	
	/// <summary>
	/// Format 0.0000 (buildnumber.changesetid)
	/// </summary>
	public string BuildVersion { get; protected set; } = string.Empty;
	
	/// <summary>
	/// Static method for created prebuild class from config type
	/// </summary>
	/// <param name="preBuildType"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static PreBuildBase Create(PreBuildType preBuildType)
	{
		return preBuildType switch
		{
			PreBuildType.None => new PreBuild_None(),
			PreBuildType.Major => new PreBuild_Major(),
			PreBuildType.Major_Minor => new PreBuild_Major_Minor(),
			PreBuildType.Major_ChangeSetId => new PreBuild_Major_ChangeSetId(),
			_ => throw new ArgumentOutOfRangeException(nameof(preBuildType), preBuildType, null)
		};
	}

	/// <summary>
	/// Gets the current changeSetId
	/// </summary>
	/// <returns></returns>
	private static int GetCurrentChangeSetId()
	{
		var cmdRes = Cmd.Run("cm", "find changeset \"where branch='main'\" \"order by date desc\" \"limit 1\" --format=\"{changesetid}\" --nototal");
		return int.TryParse(cmdRes.output, out var id) ? id : 0;
	}

	/// <summary>
	/// Gets previous changeSetId based on commit message
	/// </summary>
	/// <returns></returns>
	private static int GetPreviousChangeSetId()
	{
		var str = File.Exists(PREV_CHANGESET_ID_PATH)
			? File.ReadAllText(PREV_CHANGESET_ID_PATH)
			: "0";
		return int.TryParse(str, out var id) ? id : 0;
	}

	public virtual void Run()
	{
		// get previously store change set value
		CurrentChangeSetId = GetCurrentChangeSetId();

		// get current change set number from plastic
		Logger.Log($"{nameof(PreviousChangeSetId)}: {PreviousChangeSetId}");
		Logger.Log($"{nameof(CurrentChangeSetId)}: {CurrentChangeSetId}");
	}

	/// <summary>
	/// Gets all change logs between two changeSetIds
	/// </summary>
	public string[] GetChangeLog(bool print = true)
	{
		var raw = Cmd.Run("cm", $"log --from={PreviousChangeSetId} cs:{CurrentChangeSetId} --csformat=\"{{comment}}\"").output;
		var changeLog = raw.Split(Environment.NewLine).Reverse().ToArray();
		
		if (print)
			Logger.Log($"___Change Logs___\n{string.Join("\n", changeLog)}");
		
		return changeLog;
	}

	public void CommitNewVersionNumber(string messagePrefix = "Build Version")
	{
		if (string.IsNullOrEmpty(BuildVersion))
			return;
		
		// write new prev changeset id
		File.WriteAllText(PREV_CHANGESET_ID_PATH, CurrentChangeSetId.ToString());

		// update in case there are new changes in coming otherwise it will fail
		Cmd.Run("cm", "update");
		
		/*
		 * checkin files:
		 *		- project settings
		 *		- steam vdfs
		 *		- previous changeset id, 
		 */
		var filesToCommit = new List<string>
		{
			PROJECT_SETTINGS,
			PREV_CHANGESET_ID_PATH,
		};
		
		// add vdfs
		var vdfs = new DirectoryInfo(STEAM_DIR_PATH).GetFiles("*.vdf");
		var relativeNames = vdfs.Select(x => x.FullName.Replace($"{Environment.CurrentDirectory}/", string.Empty));
		filesToCommit.AddRange(relativeNames);
		
		// commit changes
		var filesStr = string.Join(" ", filesToCommit);
		var fullCommitMessage = $"{messagePrefix}: {BuildVersion}";
		Logger.Log($"Commiting new build version \"{fullCommitMessage}\"");
		Cmd.Run("cm", $"ci {filesStr} -c=\"{fullCommitMessage}\"");
	}

	private static string GetAppVersion()
	{
		var appVer = File.ReadAllLines(PROJECT_SETTINGS)
			.Single(x => x.Contains("bundleVersion:"))
			.Replace("bundleVersion: ", string.Empty)
			.Trim();
		return appVer;
	}
	
	protected static int[] GetVersionArray()
	{
		var verStr = GetAppVersion();
		var ver = verStr.Split(".");
		var arr = new int[ver.Length];

		for (int i = 0; i < ver.Length; i++)
			arr[i] = int.Parse(ver[i].Trim());

		return arr;
	}
	
	/// <summary>
	/// Replaces the version in all the places within ProjectSettings.asset
	/// </summary>
	/// <param name="newBundleVersion"></param>
	public static void ReplaceVersions(string? newBundleVersion)
	{
		var lines = File.ReadAllLines(PROJECT_SETTINGS);

		var isBundleVersionFound = false;
		var isBuildNumFound = false;
		var isBuildNumStandaloneFound = false;

		for (int i = 0; i < lines.Length; i++)
		{
			// bundle version
			if (!isBundleVersionFound && lines[i].Contains("bundleVersion:"))
			{
				lines[i] = ReplaceText(lines[i], newBundleVersion);
				isBundleVersionFound = true;
			}
			
			// build number
			if (!isBuildNumFound && lines[i].Contains("buildNumber:"))
				isBuildNumFound = true;
			
			if (!isBuildNumFound)
				continue;

			if (!isBuildNumStandaloneFound && lines[i].Contains("Standalone:"))
			{
				lines[i] = ReplaceText(lines[i], newBundleVersion);
				isBuildNumStandaloneFound = true;
			}
		}

		File.WriteAllText(PROJECT_SETTINGS, string.Join("\n", lines));
	}
	
	private static string ReplaceText(string line, string version)
	{
		var ver = line.Split(":").Last().Trim();
		var replacement = line.Replace(ver, version);
		return replacement;
	}
}
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
	
	public int CurrentChangeSetId { get; protected set; }
	public int PreviousChangeSetId { get; private set; }
	
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
		var changeSetStr = Cmd.Run("cm", "find changeset \"where branch='main'\" \"order by date desc\" \"limit 1\" --format=\"{changesetid}\" --nototal");
		return int.TryParse(changeSetStr.output, out var id) ? id : 0;
	}

	/// <summary>
	/// Gets previous changeSetId based on commit message
	/// </summary>
	/// <returns></returns>
	private static int GetPreviousChangeSetId()
	{
		var changeSetIds = GetChangeChangSetIdsLastBuildVersions(1);
		return changeSetIds[0];
	}

	private static int[] GetChangeChangSetIdsLastBuildVersions(int limit, string commentLike = "Build Version")
	{
		var cmdRes = Cmd.Run("cm", $"find changeset \"where branch='main' and comment like '%{commentLike}%'\" \"order by date desc\" \"limit {limit}\" --format=\"{{changesetid}}\" --nototal");
		var stdOut = cmdRes.output;
		var lines = stdOut.Split(Environment.NewLine);
		var changesetIds = new int[lines.Length];
		for (int i = 0; i < lines.Length; i++)
			changesetIds[i] = int.TryParse(lines[i], out var id) ? id : 0;
		return changesetIds;
	}
	
	public virtual void Run()
	{
		// get previously store change set value
		PreviousChangeSetId = GetPreviousChangeSetId();
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
		var changeSetIds = GetChangeChangSetIdsLastBuildVersions(2);
		var csFrom = changeSetIds[^1];
		var raw = Cmd.Run("cm", $"log --from={csFrom} cs:{CurrentChangeSetId} --csformat=\"{{comment}}\"").output;
		var changeLog = raw.Split(Environment.NewLine).Reverse().ToArray();
		
		if (print)
			Logger.Log($"___Change Logs___\n{string.Join("\n", changeLog)}");
		
		return changeLog;
	}

	public void CommitNewVersionNumber(string messagePrefix = "Build Version")
	{
		if (string.IsNullOrEmpty(BuildVersion))
			return;

		// update in case there are new changes in coming  
		Cmd.Run("cm", "update");
		
		var fullCommitMessage = $"{messagePrefix}: {BuildVersion}";
		Logger.Log($"Commiting new build version \"{fullCommitMessage}\"");
		Cmd.Run("cm", $"ci {PROJECT_SETTINGS} -c=\"{fullCommitMessage}\"");
		
		// update new changeSetId
		CurrentChangeSetId = GetCurrentChangeSetId();
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
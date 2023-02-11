using Deployment.Misc;
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
	
	/// <summary>
	/// All raw commit messages
	/// </summary>
	public string[] ChangeLog { get; private set; } = Array.Empty<string>();

	protected int PreviousChangeSetId { get; private set; }
	
	/// <summary>
	/// Format 0.0000 (buildnumber.changesetid)
	/// </summary>
	public string BuildVersion { get; protected set; } = string.Empty;
	
	public bool IsRun { get; private set; }
	
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
			PreBuildType.Major => new PreBuild_Major_Minor(0),
			PreBuildType.Major_Minor => new PreBuild_Major_Minor(1),
			PreBuildType.Major_ChangeSetId => new PreBuild_Major_ChangeSetId(),
			_ => throw new ArgumentOutOfRangeException(nameof(preBuildType), preBuildType, null)
		};
	}
	
	public virtual void Run()
	{
		Cmd.Run("cm", "unco -a"); // clear workspace
		Cmd.Run("cm", "upd"); // update workspace
		
		// get previously store change set value
		var prevChangeSetIdStr = Cmd.Run("cm", "find changeset \"where branch='main' and comment like '%Build Version%'\" \"order by date desc\" \"limit 1\" --format=\"{changesetid}\" --nototal");
		PreviousChangeSetId = int.TryParse(prevChangeSetIdStr.output, out var id) ? id : 0;
		
		IsRun = true;
	}

	public void SetChangeLog()
	{
		var raw = Cmd.Run("cm", $"log --from=cs:{PreviousChangeSetId} --csformat=\"{{comment}}\"").output;
		var array = raw.Split(Environment.NewLine).Reverse().ToArray();
		ChangeLog = array;
	}

	public void CommitNewVersionNumber()
	{
		Cmd.Run("cm", $"ci {PROJECT_SETTINGS} -c=\"Build Version: {BuildVersion}\"");
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
	
	protected static void ReplaceVersions(string newBundleVersion)
	{
		var text = File.ReadAllText(PROJECT_SETTINGS);
		var lines = text.Split("\n");

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

		var output = string.Join("\n", lines);
		File.WriteAllText(PROJECT_SETTINGS, output);
	}
	
	private static string ReplaceText(string line, string version)
	{
		var ver = line.Split(":").Last().Trim();
		var replacement = line.Replace(ver, version);
		return replacement;
	}
}
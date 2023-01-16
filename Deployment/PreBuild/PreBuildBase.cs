using Deployment.Misc;

namespace Deployment.PreBuild;

public enum PreBuildType
{
	/// <summary>
	/// No prebuild
	/// </summary>
	None,
	
	/// <summary>
	/// MAJOR.MINOR
	/// </summary>
	Major_Minor,
	
	/// <summary>
	/// MAJOR.CHANGE_SET_ID
	/// </summary>
	Major_ChangeSetId
}

public abstract class PreBuildBase
{
	private const string PROJECT_SETTINGS = "ProjectSettings/ProjectSettings.asset";
	
	/// <summary>
	/// All raw commit messages
	/// </summary>
	public string ChangeLog { get; protected set; } = string.Empty;
	
	/// <summary>
	/// Format 0.0000 (buildnumber.changesetid)
	/// </summary>
	public string BuildVersion { get; protected set; } = string.Empty;
	
	public bool IsRun { get; private set; }
	
	public virtual void Run()
	{
		Cmd.Run("cm", "unco -a"); // clear workspace
		Cmd.Run("cm", "upd"); // update workspace
		IsRun = true;
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
		
		File.WriteAllLines(PROJECT_SETTINGS, lines);
	}
	
	private static string ReplaceText(string line, string version)
	{
		var ver = line.Split(":").Last().Trim();
		var replacement = line.Replace(ver, version);
		return replacement;
	}
}
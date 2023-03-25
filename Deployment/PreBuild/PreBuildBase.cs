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
	
	// TODO: add MAJOR_MINOR_PATCH
}

public abstract class PreBuildBase
{ 
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
			_ => throw new ArgumentOutOfRangeException(nameof(preBuildType), preBuildType, null)
		};
	}

	public abstract void Run();

	public static string GetAppVersion()
	{
		var appVer = File.ReadAllLines(Workspace.PROJECT_SETTINGS)
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
		var lines = File.ReadAllLines(Workspace.PROJECT_SETTINGS);

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

		File.WriteAllText(Workspace.PROJECT_SETTINGS, string.Join("\n", lines));
	}
	
	private static string ReplaceText(string line, string version)
	{
		var ver = line.Split(":").Last().Trim();
		var replacement = line.Replace(ver, version);
		return replacement;
	}
}
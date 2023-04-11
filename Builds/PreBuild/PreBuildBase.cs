using Deployment.Configs;
using SharedLib;

namespace Builds.PreBuild;

public abstract class PreBuildBase
{


	/// <summary>
	/// Static method for created prebuild class from config type
	/// </summary>
	/// <param name="preBuildType"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static PreBuildBase Create(PreBuildType preBuildType, Workspace workspace)
	{
		return preBuildType switch
		{
			PreBuildType.None => new PreBuild_None(workspace),
			PreBuildType.Major => new PreBuild_Major(workspace),
			PreBuildType.Major_Minor => new PreBuild_Major_Minor(workspace),
			_ => throw new ArgumentOutOfRangeException(nameof(preBuildType), preBuildType, null)
		};
	}

	protected readonly Workspace _workspace;
	
	/// <summary>
	/// Format 0.0000 (buildnumber.changesetid)
	/// </summary>
	public string BuildVersion { get; protected set; } = string.Empty;

	public PreBuildBase(Workspace workspace)
	{
		_workspace = workspace;
	}

	public abstract void Run();

	/// <summary>
	/// Replaces the version in all the places within ProjectSettings.asset
	/// </summary>
	public static void ReplaceVersions(string? newBundleVersion, string projectSettingsPath = Workspace.PROJECT_SETTINGS)
	{
		var lines = File.ReadAllLines(projectSettingsPath);

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
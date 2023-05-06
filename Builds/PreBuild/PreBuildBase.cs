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
	protected readonly ProjectSettingsWriter _projectSettingsWriter;
	
	/// <summary>
	/// Format 0.0000 (buildnumber.changesetid)
	/// </summary>
	public string BuildVersion { get; protected set; } = string.Empty;

	public PreBuildBase(Workspace workspace)
	{
		_workspace = workspace;
		_projectSettingsWriter = new ProjectSettingsWriter(workspace.ProjectSettingsPath);
	}

	public abstract void Run();
}
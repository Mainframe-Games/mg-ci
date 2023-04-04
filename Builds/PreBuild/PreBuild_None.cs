using SharedLib;

namespace Builds.PreBuild;

/// <summary>
/// Empty class to use for None. Only base class stuff will run.
/// </summary>
public class PreBuild_None : PreBuildBase
{
	public PreBuild_None(Workspace workspace) : base(workspace)
	{
	}

	public override void Run()
	{
	}
}
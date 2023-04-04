using SharedLib;

namespace Builds.PreBuild;

public class PreBuild_Major : PreBuildBase
{
	public PreBuild_Major(Workspace workspace) : base(workspace)
	{
	}

	public override void Run()
	{
		// bump patch
		var arr = _workspace.GetVersionArray();
		arr[0]++;
		BuildVersion = arr[0].ToString();

		// apply changes
		ReplaceVersions(BuildVersion);
	}
}
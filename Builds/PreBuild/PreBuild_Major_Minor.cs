using SharedLib;

namespace Builds.PreBuild;

public class PreBuild_Major_Minor : PreBuildBase
{
	public PreBuild_Major_Minor(Workspace workspace) : base(workspace)
	{
	}
	
	public override void Run()
	{
		// bump patch
		var arr = _workspace.GetVersionArray();
		arr[1]++;
		BuildVersion = string.Join(".", arr);
		
		// apply changes
		_projectSettingsWriter.ReplaceVersions(BuildVersion);
	}
}
namespace Deployment.PreBuild;

public class PreBuild_Major_Minor : PreBuildBase
{
	public override void Run()
	{
		// bump patch
		var arr = GetVersionArray();
		arr[1]++;
		BuildVersion = string.Join(".", arr);
		
		// apply changes
		ReplaceVersions(BuildVersion);
	}
}
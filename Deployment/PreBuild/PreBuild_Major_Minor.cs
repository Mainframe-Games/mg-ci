namespace Deployment.PreBuild;

public class PreBuild_Major_Minor : PreBuildBase
{
	public override void Run()
	{
		base.Run();
		
		// bump patch
		var arr = GetVersionArray();
		arr[1]++;
		BuildVersion = string.Join(".", arr);
		
		// apply changes
		ReplaceVersions(BuildVersion);
	}
}
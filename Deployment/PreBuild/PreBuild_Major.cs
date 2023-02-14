namespace Deployment.PreBuild;

public class PreBuild_Major : PreBuildBase
{
	public override void Run()
	{
		base.Run();
		
		// bump patch
		var arr = GetVersionArray();
		arr[0]++;
		BuildVersion = arr[0].ToString();
		
		// apply changes
		ReplaceVersions(BuildVersion);
	}
}
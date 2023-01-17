namespace Deployment.PreBuild;

public class PreBuild_Major_Minor : PreBuildBase
{
	private readonly int _index;
	
	public PreBuild_Major_Minor(int index)
	{
		_index = index;
	}
	
	public override void Run()
	{
		base.Run();
		
		// bump patch
		var arr = GetVersionArray();
		arr[_index]++;
		BuildVersion = string.Join(".", arr);
		
		// apply changes
		ReplaceVersions(BuildVersion);
	}
}
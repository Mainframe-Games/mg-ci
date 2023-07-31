namespace SharedLib;

public class BuildSettingsAsset : Yaml
{
	public string FileName { get; }
	public string Name => GetValue<string>(nameof(Name));
	public string BuildPath => GetValue<string>(nameof(BuildPath));
	public BuildTarget Target => (BuildTarget)GetValue<int>(nameof(Target));
	public UnitySubTarget SubTarget => (UnitySubTarget)GetValue<int>(nameof(SubTarget));
	public UnityBuildTargetGroup TargetGroup => (UnityBuildTargetGroup)GetValue<int>(nameof(TargetGroup));
	
	public BuildSettingsAsset(string? path) : base(path)
	{
		var info = new FileInfo(path);
		FileName = info.Name.Replace(info.Extension, string.Empty);
	}

	public override T GetValue<T>(string path)
	{
		return base.GetValue<T>($"MonoBehaviour.{path}");
	}

	public BuildTargetFlag GetBuildTargetFlag()
	{
		switch (Target)
		{
			case BuildTarget.StandaloneOSX:
			// case BuildTarget.StandaloneOSXUniversal:
			case BuildTarget.StandaloneOSXIntel:
			case BuildTarget.StandaloneOSXIntel64:
				return BuildTargetFlag.OSXUniversal;
			
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				return BuildTargetFlag.Win64;
			
			case BuildTarget.EmbeddedLinux:
			case BuildTarget.StandaloneLinux:
			case BuildTarget.StandaloneLinux64:
			case BuildTarget.StandaloneLinuxUniversal:
				return BuildTargetFlag.Linux64;
			
			case BuildTarget.iOS:
				return BuildTargetFlag.iOS;
			
			case BuildTarget.Android:
				return BuildTargetFlag.Android;
		}

		throw new NotSupportedException($"Target not supported: {Target}");
	}
}
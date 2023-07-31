namespace SharedLib;

public class BuildSettingsAsset : Yaml
{
	public string FileName { get; }
	public string Name => GetValue<string>(nameof(Name));
	public string BuildPath => GetValue<string>(nameof(BuildPath));
	public UnityTarget Target => (UnityTarget)GetValue<int>(nameof(Target));
	public UnitySubTarget SubTarget => (UnitySubTarget)GetValue<int>(nameof(SubTarget));
	public UnityBuildTargetGroup BuildTargetGroup => (UnityBuildTargetGroup)GetValue<int>(nameof(BuildTargetGroup));
	
	public BuildSettingsAsset(string? path) : base(path)
	{
		var info = new FileInfo(path);
		FileName = info.Name.Replace(info.Extension, string.Empty);
	}

	public override T GetValue<T>(string path)
	{
		return base.GetValue<T>($"MonoBehaviour.{path}");
	}
}
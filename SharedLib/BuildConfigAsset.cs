namespace SharedLib;

public class BuildConfigAsset : Yaml
{
	public BuildConfigAsset(string? path, int skip = 3) : base(path, skip)
	{
	}

	public override T GetValue<T>(string path)
	{
		return base.GetValue<T>($"MonoBehaviour.{path}");
	}

	public override T? GetObject<T>(string path) where T : class
	{
		return base.GetObject<T>($"MonoBehaviour.{path}");
	}
}
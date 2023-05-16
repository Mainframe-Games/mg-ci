using SharedLib;

namespace Deployment.Configs;

public class BuildSettingsAsset : Yaml
{
	public BuildSettingsAsset(string? path) : base(path)
	{
	}

	public override T GetValue<T>(string path)
	{
		return base.GetValue<T>($"MonoBehaviour.{path}");
	}
}
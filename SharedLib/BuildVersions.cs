using Newtonsoft.Json;

namespace SharedLib;

public class BuildVersions
{
	public string? BundleVersion { get; set; }
	public string? AndroidVersionCode { get; set; }
	public string? Standalone { get; set; }
	public string? IPhone { get; set; }

	/// <summary>
	/// Includes BundleVersion + build version
	/// </summary>
	[JsonIgnore] public string FullVersion => $"{BundleVersion}.{Standalone}";

	public override string ToString()
	{
		return Json.Serialise(this);
	}
}
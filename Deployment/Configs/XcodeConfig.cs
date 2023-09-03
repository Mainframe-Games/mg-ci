using SharedLib;

namespace Deployment.Configs;

public class XcodeConfig
{
	public string? AppleId { get; set; }
	public string? AppSpecificPassword { get; set; }

	public static XcodeConfig? FromObject(object obj)
	{
		var jsonStr = Json.Serialise(obj);
		return Json.Deserialise<XcodeConfig>(jsonStr);
	}
}
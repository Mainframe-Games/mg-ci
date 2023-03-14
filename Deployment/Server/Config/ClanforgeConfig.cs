namespace Deployment.Server.Config;

public class ClanforgeConfig
{
	public string? AccessKey { get; set; }
	public string? SecretKey { get; set; }
	public uint Asid { get; set; }
	public uint MachineId { get; set; }
	public uint ImageId { get; set; }
	public string? Url { get; set; }
	public Dictionary<string, string>? ImageIdProfileNames { get; set; }

	public string GetProfileName(uint imageId)
	{
		if (ImageIdProfileNames == null)
			return string.Empty;
		
		foreach (var idProfileName in ImageIdProfileNames)
		{
			var id = uint.Parse(idProfileName.Key);
			
			if (imageId == id)
				return idProfileName.Value;
		}

		return string.Empty;
	}

	public static string BuildHookMessage(ClanforgeConfig? config, string status)
	{
		if (config == null)
			return string.Empty;
		
		var profileName = config.GetProfileName(config.ImageId);
		return $"Game Image {status}: {profileName} ({config.ImageId})";
	}
}
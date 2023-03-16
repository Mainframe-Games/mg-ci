using System.Text;

namespace Deployment.Server.Config;

public class ClanforgeConfig
{
	public string? AccessKey { get; set; }
	public string? SecretKey { get; set; }
	public uint Asid { get; set; }
	public uint MachineId { get; set; }
	public uint[]? ImageIds { get; set; }
	public string? Url { get; set; }
	public Dictionary<string, string>? ImageIdProfileNames { get; set; }

	private string GetProfileName(uint imageId)
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

	public string BuildHookMessage(string status)
	{
		if (ImageIds == null)
			return string.Empty;
		
		var str = new StringBuilder();
		
		foreach (var imageId in ImageIds)
		{
			var profileName = GetProfileName(imageId);
			str.AppendLine($"Game Image {status}: {profileName} ({imageId})");
		}
		
		return str.ToString();
	}
}
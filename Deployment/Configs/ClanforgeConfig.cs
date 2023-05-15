using System.Text;
using Newtonsoft.Json;

namespace Deployment.Configs;

public class ClanforgeConfig
{
	public string? AccessKey { get; set; }
	public string? SecretKey { get; set; }
	public uint Asid { get; set; }
	public uint MachineId { get; set; }
	public string? Url { get; set; }
	public bool IsProduction { get; set; }
	public Dictionary<string, string>? ImageIdProfileNames { get; set; }

	[JsonIgnore] private string Tag => IsProduction ? "Production" : "Development";

	public string BuildHookMessage(string status)
	{
		var str = new StringBuilder();
		
		foreach (var (imageId, profileName) in ImageIdProfileNames)
		{
			if (profileName.Contains(Tag))
				str.AppendLine($"Game Image {status}: {profileName} ({imageId})");
		}
		
		return str.ToString();
	}

	public IEnumerable<uint> GetImageIds()
	{
		foreach (var (imageId, profileName) in ImageIdProfileNames)
		{
			if (profileName.Contains(Tag))
				yield return uint.Parse(imageId);
		}
	}
}
using System.Text;

namespace Deployment.Configs;

public class ClanforgeConfig
{
	public string? AccessKey { get; set; }
	public string? SecretKey { get; set; }
	public uint Asid { get; set; }
	public uint MachineId { get; set; }
	public bool IsProduction { get; set; }
	public Dictionary<string, ClanforgeProfile> Profiles { get; set; }

	private ClanforgeProfile GetProfile()
	{
		var profile = IsProduction ? "production" : "development";
		
		if (Profiles.TryGetValue(profile, out var p))
			return p;

		throw new KeyNotFoundException($"Profile not found: {profile}");
	}

	public string BuildHookMessage(string status)
	{
		var str = new StringBuilder();
		var profile = GetProfile();
		
		foreach (var image in profile.Images)
			str.AppendLine($"Game Image {status}: {image.Name} ({image.Id})");
		
		return str.ToString();
	}

	public IEnumerable<uint>? GetImageIds()
	{
		return GetProfile().Images?.Select(x => x.Id);
	}

	public string? GetUrl()
	{
		return GetProfile().Url;
	}
}

public class ClanforgeProfile
{
	public string? Url { get; set; }
	public ClanforgeImage[]? Images { get; set; }
}

public class ClanforgeImage
{
	public uint Id { get; set; }
	public string? Name;
}
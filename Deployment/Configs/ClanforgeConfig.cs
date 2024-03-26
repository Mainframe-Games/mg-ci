using System.Web;
using SharedLib;

namespace Deployment.Configs;

public class ClanforgeConfig 
{
	public string? AccessKey { get; set; }
	public string? SecretKey { get; set; }
	public uint Asid { get; set; }
	public uint MachineId { get; set; }
	public string? Url { get; set; }
	public string? DefaultProfile { get; set; }
	public Dictionary<string, ClanforgeProfile>? Profiles { get; set; }

	private ClanforgeProfile GetProfile(string? profile)
	{
		if (Profiles?.TryGetValue(profile, out var p) is true)
			return p;

		throw new KeyNotFoundException($"Profile not found: {profile}");
	}

	public string BuildHookMessage(string? profileId, string status)
	{
		var profile = GetProfile(profileId);
		return $"**Clanforge Image {status}**: {profile.Name} ({profile.Id})";
	}

	public uint GetImageId(string? profileId)
	{
		return GetProfile(profileId).Id;
	}

	public string GetUrl(string? beta)
	{
		if (string.IsNullOrEmpty(beta))
			return Url ?? string.Empty;

		var uriBuilder = new UriBuilder(Url ?? string.Empty);
		var query = HttpUtility.ParseQueryString(uriBuilder.Query);
		query["beta"] = beta;
		uriBuilder.Query = query.ToString();
		return uriBuilder.ToString();
	}

	public ClanforgeConfig Clone()
	{
		return new ClanforgeConfig
		{
			AccessKey = AccessKey,
			SecretKey = SecretKey,
			Asid = Asid,
			MachineId = MachineId,
			Url = Url,
			DefaultProfile = DefaultProfile,
			Profiles = Profiles,
		};
	}

	public override string ToString()
	{
		return Json.Serialise(this);
	}
}

public class ClanforgeProfile
{
	public uint Id { get; set; }
	public string? Name { get; set; }
}


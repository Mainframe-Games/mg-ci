using System.Web;
using SharedLib;

namespace Deployment.Configs;

public class ClanforgeConfig : ICloneable
{
	public string? AccessKey { get; set; }
	public string? SecretKey { get; set; }
	public uint Asid { get; set; }
	public uint MachineId { get; set; }
	public string? Url { get; set; }
	public Dictionary<string, ClanforgeProfile> Profiles { get; set; }

	private ClanforgeProfile GetProfile(string? profile)
	{
		if (Profiles.TryGetValue(profile, out var p))
			return p;

		throw new KeyNotFoundException($"Profile not found: {profile}");
	}

	public string BuildHookMessage(string? profileId, string status)
	{
		var profile = GetProfile(profileId);
		return $"Game Image {status}: {profile.Name} ({profile.Id})";
	}

	public uint GetImageId(string? profileId)
	{
		return GetProfile(profileId).Id;
	}

	public string GetUrl(string? branch)
	{
		if (string.IsNullOrEmpty(branch))
			return Url ?? string.Empty;

		var uriBuilder = new UriBuilder(Url ?? string.Empty);
		var query = HttpUtility.ParseQueryString(uriBuilder.Query);
		query["beta"] = branch;
		uriBuilder.Query = query.ToString();
		return uriBuilder.ToString();
	}

	public object Clone()
	{
		var str = Json.Serialise(this);
		return Json.Deserialise<ClanforgeConfig>(str) ?? new ClanforgeConfig();
	}
}

public class ClanforgeProfile
{
	public uint Id { get; set; }
	public string? Name { get; set; }
}


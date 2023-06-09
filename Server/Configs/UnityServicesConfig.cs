namespace Server.Configs;

public class UnityServicesConfig
{
	public string? AccessKey { get; set; }
	public string? SecretKey { get; set; }
	public string? ProjectId { get; set; }
	public string? EnvironmentId { get; set; }
	public UnityRemoteConfigConfig? RemoteConfig { get; set; }
	public UnityGameServerHostingConfig ServerHosting { get; set; }

	public string BuildUrl(string pathRoot, string endpoint)
	{
		return $"https://services.api.unity.com/{pathRoot}/v1/projects/{ProjectId}/{endpoint}";
	}
}

public class UnityRemoteConfigConfig
{
	public string? ConfigId { get; set; }
	public string? ValueKey { get; set; }
}

public class UnityGameServerHostingConfig
{
	public int BuildId { get; set; }
}
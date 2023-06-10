namespace Server.Configs;

public class UnityServicesConfig
{
	public string? KeyId { get; set; }
	public string? SecretKey { get; set; }
	public UnityProject[]? Projects { get; set; }
	public UnityRemoteConfigConfig? RemoteConfig { get; set; }
	public UnityGameServerHostingConfig? ServerHosting { get; set; }

	public UnityProject? GetProjectFromName(string projName)
	{
		return Projects?.First(x => x.Name == projName);
	}
}

public class UnityRemoteConfigConfig
{
	public string? ConfigId { get; set; }
	public string? ValueKey { get; set; }
}

public class UnityGameServerHostingConfig
{
	public ulong BuildId { get; set; }
}

public class UnityProject
{
	public string? Name { get; set; }
	public string? ProjectId { get; set; }
	public string? EnvironmentId { get; set; }
}
using Newtonsoft.Json;
using SharedLib;

namespace DiscordBot.Configs;

public class DiscordConfig
{
    private static string ConfigPath => Args.Environment.TryGetArg("-config", out var configPath)
	    ? configPath 
	    : "config-discord.json";

    public string? BuildServerUrl { get; set; }
	public string? Token { get; set; }
	/// <summary>
	/// Channel Id
	/// </summary>
	public ulong GuildId { get; set; }
	public List<string>? AuthorisedRoles { get; set; }
	public string? CommandName { get; set; }
	
	[JsonIgnore]
	public List<string?>? WorkspaceNames { get; private set; }

	public static DiscordConfig? Load()
	{
		var configStr = File.ReadAllText(ConfigPath);
		var config = Json.Deserialise<DiscordConfig>(configStr);
		
		if (config != null)
			config.WorkspaceNames = Workspace.GetAvailableWorkspaces().Select(x => x.Name).ToList();
		
		return config;
	}

	public void Refresh()
	{
		var updated = Load();
		
		if (updated == null)
			return;
		
		BuildServerUrl = updated.BuildServerUrl;
		CommandName = updated.CommandName;
		AuthorisedRoles = updated.AuthorisedRoles;
		WorkspaceNames = updated.WorkspaceNames;
	}
}
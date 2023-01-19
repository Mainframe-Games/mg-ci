using SharedLib;

namespace DiscordBot.Configs;

public class DiscordConfig
{
    private static string ConfigPath => Args.Environment.TryGetArg("-config", out var configPath)
	    ? configPath 
	    : "config-discord.json";

    public string? BuildServerUrl { get; set; }
	public string? Token { get; set; }
	public ulong GuildId { get; set; }
	public List<string>? AuthorisedRoles { get; set; }
	public List<ChannelWrap>? Workspaces { get; set; }
	public string? CommandName { get; set; }

	public static DiscordConfig? Load()
	{
		var configStr = File.ReadAllText(ConfigPath);
		var config = Json.Deserialise<DiscordConfig>(configStr);
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
		Workspaces = updated.Workspaces;
	}
}
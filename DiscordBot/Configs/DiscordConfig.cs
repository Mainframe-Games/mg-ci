using Newtonsoft.Json;
using SharedLib;

namespace DiscordBot.Configs;

public class DiscordConfig
{
    private static string ConfigPath => Args.Environment.TryGetArg("-config", out var configPath)
	    ? configPath 
	    : "config-discord.json";

    public string? BuildServerUrl { get; set; } = "http://127.0.0.1:8080";
	public string? Token { get; set; }
	/// <summary>
	/// Channel Id
	/// </summary>
	public ulong GuildId { get; set; }
	public List<string>? AuthorisedRoles { get; set; }
	public string? CommandName { get; set; } = "start-build";

	[JsonIgnore] public List<string> Workspaces { get; private set; } = new();

	public static async Task<DiscordConfig?> LoadAsync()
	{
		var configStr = await File.ReadAllTextAsync(ConfigPath);
		var config = Json.Deserialise<DiscordConfig>(configStr);
		await config.SetWorkspaceNamesAsync();
		return config;
	}

	public async Task SetWorkspaceNamesAsync()
	{
		if (string.IsNullOrEmpty(BuildServerUrl) || Args.Environment.IsFlag("-local"))
		{
			Workspaces = Workspace.GetAvailableWorkspaces().Select(x => x.Name).ToList();
			return;
		}

		try
		{
			var res = await Web.SendAsync(HttpMethod.Get, $"{BuildServerUrl}/workspaces");
			Workspaces = Json.Deserialise<List<string>>(res.Content) ?? new List<string>();
		}
		catch (HttpRequestException)
		{
			Logger.Log($"Connection to '{BuildServerUrl}' count not be made");
		}
	}
}
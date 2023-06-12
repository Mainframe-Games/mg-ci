using System.Net;
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

	public static async Task<DiscordConfig?> LoadAsync()
	{
		var configStr = await File.ReadAllTextAsync(ConfigPath);
		var config = Json.Deserialise<DiscordConfig>(configStr);
		await config.SetWorkspaceNamesAsync();
		return config;
	}

	public async Task RefreshAsync()
	{
		var updated = await LoadAsync();
		
		if (updated == null)
			return;
		
		BuildServerUrl = updated.BuildServerUrl;
		CommandName = updated.CommandName;
		AuthorisedRoles = updated.AuthorisedRoles;
		WorkspaceNames = updated.WorkspaceNames;
	}

	private async Task SetWorkspaceNamesAsync()
	{
		if (string.IsNullOrEmpty(BuildServerUrl) || Args.Environment.IsFlag("-local"))
		{
			WorkspaceNames = Workspace.GetAvailableWorkspaces().Select(x => x.Name).ToList();
			return;
		}
		
		var res = await Web.SendAsync(HttpMethod.Get, $"{BuildServerUrl}/workspaces");
		
		if (res.StatusCode != HttpStatusCode.OK)
			throw new WebException(res.Content);
		
		var list = Json.Deserialise<List<string?>?>(res.Content);
		WorkspaceNames = list;
	}
}
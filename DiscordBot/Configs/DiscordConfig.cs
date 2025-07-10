using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharedLib;

namespace DiscordBot.Configs;

public class DiscordConfig
{
    private static string ConfigPath =>
        Args.Environment.TryGetArg("-config", out var configPath)
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
    public ListenServerConfig? ListenServer { get; set; }
    public List<Reminder>? Reminders { get; set; }

    public async Task SaveAsync()
    {
        var config = Json.Serialise(this);
        await File.WriteAllTextAsync(ConfigPath, config);
    }

    public static DiscordConfig Load()
    {
        var configStr = File.ReadAllText(ConfigPath);
        var config = Json.Deserialise<DiscordConfig>(configStr);

        if (config == null)
            throw new NullReferenceException("Config is null");

        return config;
    }

    public async Task<List<string>> SetWorkspaceNamesAsync()
    {
        // if (string.IsNullOrEmpty(BuildServerUrl) || Args.Environment.IsFlag("-local"))
        // return PlasticWorkspace.GetAvailableWorkspaces().Select(x => x.Name).ToList();

        try
        {
            var res = await Web.SendAsync(HttpMethod.Get, $"{BuildServerUrl}/workspaces");
            var workspaces = Json.Deserialise<List<string>>(res.Content) ?? new List<string>();
            return workspaces;
        }
        catch (HttpRequestException)
        {
            Logger.Log($"Connection to '{BuildServerUrl}' count not be made");
        }

        return new List<string>();
    }
}

public class ListenServerConfig
{
    public string? Ip { get; set; }
    public ushort Port { get; set; }

    [JsonIgnore]
    public string? Address => $"http://{Ip}:{Port}";
}

public class Reminder
{
    public string Name { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public ulong ChannelId { get; set; }
    public string? Message { get; set; }

    public override string ToString()
    {
        return $"{Name} @ {Hour}:{Minute} to channel '{ChannelId}'";
    }
}

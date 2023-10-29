using System.Net;
using Deployment.Configs;
using Deployment.Deployments;
using Server.Configs;
using SharedLib;
using SharedLib.Server;
using SharedLib.Webhooks;

namespace Server.RemoteBuild;

public class ClanforgeImageUpdate : IProcessable
{
	public string? Profile { get; set; }
	public string? Beta { get; set; }
	public string? Desc { get; set; }
	public bool? Full { get; set; }

	public async Task<ServerResponse> ProcessAsync()
	{
		await Task.CompletedTask;
		
		// TODO: there is a bug where Clanforge config is null, seems the whole ServerConfig is null too which should be impossible. 
		// so going to try load it again here before we continue to see if that improves.
		if (ServerConfig.Instance.Clanforge is null)
		{
			Logger.Log("[ClanforgeImageUpdate] Clanforge config was null, reloading...");
			ServerConfig.Load();
		}

		if (ServerConfig.Instance.Clanforge is null)
			return new ServerResponse(HttpStatusCode.InternalServerError, $"{nameof(ClanforgeConfig)} is null on server config. {ServerConfig.Instance}");

		var clone = ServerConfig.Instance.Clanforge.Clone();
		Logger.Log($"Clanforge config cloned: {clone}");
		ProcessInternalAsync(clone).FireAndForget();
		return new ServerResponse(HttpStatusCode.OK, this);
	}
	
	private async Task ProcessInternalAsync(ClanforgeConfig clanforgeConfig)
	{
		try
		{
			var clanforge = new ClanForgeDeploy(clanforgeConfig, Profile, Desc, Beta, Full);
			await clanforge.Deploy();
			SendHook(Desc, clanforgeConfig.BuildHookMessage(Profile, "Updated"));
			Logger.Log("ClanForgeDeploy complete");
		}
		catch (Exception e)
		{
			SendHook(clanforgeConfig.BuildHookMessage(Profile, $"Failed ({e.GetType().Name})"), e.Message, true);
		}
	}

	private static void SendHook(string? header, string? message, bool isError = false)
	{
		var Hooks = ServerConfig.Instance.Hooks;
	
		if (Hooks is null)
			return;

		foreach (var hook in Hooks)
		{
			if (hook.IsErrorChannel is true)
				continue;
			
			if (hook.IsDiscord())
			{
				var embed = new Discord.Embed
				{
					Title = header,
					Colour = isError ? Discord.Colour.RED : Discord.Colour.GREEN,
					Description = message,
					Username = hook.Title,
				};
				Discord.PostMessage(hook.Url, embed);
			}
			else if (hook.IsSlack())
			{
				Slack.PostMessage(hook.Url, $"{hook.Title} | {header}\n{message}");
			}
		}
	}
}
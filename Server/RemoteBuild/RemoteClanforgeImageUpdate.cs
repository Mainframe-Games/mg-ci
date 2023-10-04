using System.Net;
using Deployment.Configs;
using Deployment.Deployments;
using Server.Configs;
using SharedLib;
using SharedLib.Server;
using SharedLib.Webhooks;

namespace Server.RemoteBuild;

public class RemoteClanforgeImageUpdate : IProcessable
{
	public string? Profile { get; set; }
	public string? Beta { get; set; }
	public string? Desc { get; set; }

	public async Task<ServerResponse> ProcessAsync()
	{
		await Task.CompletedTask;
		ProcessInternalAsync().FireAndForget();
		return new ServerResponse(HttpStatusCode.OK, this);
	}
	
	private async Task ProcessInternalAsync()
	{
		if (ServerConfig.Instance.Clanforge is null)
			throw new NullReferenceException($"{nameof(ClanforgeConfig)} is null on server config. {ServerConfig.Instance}");
	
		var clanforgeConfig = ServerConfig.Instance.Clanforge.Clone();
		Logger.Log($"Clanforge config cloned: {clanforgeConfig}");
		
		try
		{
			var clanforge = new ClanForgeDeploy(clanforgeConfig, Profile, Desc, Beta);
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
	
		if (Hooks == null)
			return;

		foreach (var hook in Hooks)
		{
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
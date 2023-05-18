using Deployment.Configs;
using Deployment.Deployments;
using Deployment.Server;
using Server.Configs;
using SharedLib;
using SharedLib.Webhooks;

namespace Server.RemoteBuild;

public class RemoteClanforgeImageUpdate : IRemoteControllable
{
	public ClanforgeConfig? Config { get; set; }
	public string? Desc { get; set; }
	public HooksConfig[]? Hooks { get; set; }

	public ServerResponse Process()
	{
		ProcessInternalAsync().FireAndForget();
		return ServerResponse.Default;
	}

	private async Task ProcessInternalAsync()
	{
		try
		{
			var clanforge = new ClanForgeDeploy(Config, Desc);
			await clanforge.Deploy();
			SendHook(Desc, Config?.BuildHookMessage("Updated"));
			Logger.Log("ClanForgeDeploy complete");
		}
		catch (Exception e)
		{
			SendHook(Config?.BuildHookMessage($"Failed ({e.GetType().Name})"), e.Message, true);
		}
	}

	private void SendHook(string? header, string? message, bool isError = false)
	{
		Hooks ??= ServerConfig.Instance.Hooks;
	
		if (Hooks == null)
			return;

		foreach (var hook in Hooks)
		{
			if (hook.IsDiscord())
				Discord.PostMessage(hook.Url, message, hook.Title, header, isError ? Discord.Colour.RED : Discord.Colour.GREEN);
			else if (hook.IsSlack())
				Slack.PostMessage(hook.Url, $"{hook.Title} | {header}\n{message}");
		}
	}
}
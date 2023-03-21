using Deployment.Configs;
using Deployment.Deployments;
using Deployment.Misc;
using Deployment.Server.Config;
using Deployment.Webhooks;
using SharedLib;

namespace Deployment.RemoteBuild;

public class RemoteClanforgeImageUpdate : IRemoteControllable
{
	public ClanforgeConfig? Config { get; set; }
	public string? Desc { get; set; }
	public HooksConfig? Hook { get; set; }

	public async Task<string> ProcessAsync()
	{
		ProcessInternalAsync().FireAndForget();
		await Task.CompletedTask;
		return "ok";
	}

	private async Task ProcessInternalAsync()
	{
		try
		{
			var clanforge = new ClanForgeDeploy(Config, Desc);
			await clanforge.Deploy();
			SendHook(Desc, Config?.BuildHookMessage("Updated"));
		}
		catch (Exception e)
		{
			SendHook(Config?.BuildHookMessage($"Failed ({e.GetType().Name})"), e.Message, true);
		}
	}

	private void SendHook(string? header, string? message, bool isError = false)
	{
		if (Hook == null)
			return;

		if (Hook.IsDiscord())
			Discord.PostMessage(Hook.Url, message, Hook.Title, header, isError ? Discord.Colour.RED : Discord.Colour.GREEN);
		else if (Hook.IsSlack())
			Slack.PostMessage(Hook.Url, $"{Hook.Title} | {header}\n{message}");
	}
}
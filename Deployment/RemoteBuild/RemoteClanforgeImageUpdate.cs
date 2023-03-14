using Deployment.Configs;
using Deployment.Deployments;
using Deployment.Server.Config;
using Deployment.Webhooks;

namespace Deployment.RemoteBuild;

public class RemoteClanforgeImageUpdate : IRemoteControllable
{
	public ClanforgeConfig? Config { get; set; }
	public string? Desc { get; set; }
	public HooksConfig? Hook { get; set; }

	public async Task<string> ProcessAsync()
	{
		
		_ = Task.Run(ProcessInternal);
		await Task.CompletedTask;
		return "ok";
	}

	private async Task ProcessInternal()
	{
		var clanforge = new ClanForgeDeploy(Config, Desc);

		try
		{
			await clanforge.Deploy();
			SendHook(Desc, ClanforgeConfig.BuildHookMessage(Config, "Updated"));
		}
		catch (Exception e)
		{
			SendHook(ClanforgeConfig.BuildHookMessage(Config, $"Failed ({e.GetType().Name})"), e.Message, true);
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
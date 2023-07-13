using Discord.WebSocket;
using SharedLib;

namespace DiscordBot.Commands;

public class ServerInfoCommand : Command
{
	public override string? CommandName => "server-info";
	public override string? Description => "Returns version and uptime of server";
	public override async Task ExecuteAsync(SocketSlashCommand command)
	{
		await command.DeferAsync();
		var res = await Web.SendAsync(HttpMethod.Get, $"{DiscordWrapper.Config.BuildServerUrl}/info");
		await command.RespondSuccessDelayed(command.User, "Build Server Info", res.Content);
	}
}
using System.Net.Http;
using System.Threading.Tasks;
using Discord.WebSocket;
using SharedLib;

namespace DiscordBot.Commands;

public class ServerInfoCommand : Command
{
	public override string? CommandName => "server-info";
	public override string? Description => "Returns version and uptime of server";
	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		var res = await Web.SendAsync(HttpMethod.Get, $"{DiscordWrapper.Config.BuildServerUrl}/info");
		return new CommandResponse("Build Server Info", res.Content);
	}
}
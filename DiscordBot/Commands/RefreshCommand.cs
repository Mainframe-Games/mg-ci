using System;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordBot.Commands;

public class RefreshCommand : Command
{
	public static event Func<Task> OnRefreshed;
	public override string? CommandName => "refresh-workspaces";
	public override string? Description => "Refreshes the workspaces on the master server so you don't need to restart server";

	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		var workspaces = await DiscordWrapper.Config.SetWorkspaceNamesAsync();
		await OnRefreshed.Invoke();
		return new CommandResponse("Workspaces Updated", string.Join("\n", workspaces));
	}
}
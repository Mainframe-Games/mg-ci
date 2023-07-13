using Discord.WebSocket;

namespace DiscordBot.Commands;

public class RefreshCommand : Command
{
	public static event Func<Task> OnRefreshed;
	public override string? CommandName => "refresh-workspaces";
	public override string? Description => "Refreshes the workspaces on the master server so you don't need to restart server";

	public override async Task ExecuteAsync(SocketSlashCommand command)
	{
		await command.DeferAsync();
		await DiscordWrapper.Config.SetWorkspaceNamesAsync();
		await command.RespondSuccessDelayed(command.User, "Workspaces Updated", string.Join("\n", DiscordWrapper.Config.WorkspaceNames));
		await OnRefreshed.Invoke();
	}
}
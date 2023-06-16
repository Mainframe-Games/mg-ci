using Discord.WebSocket;
using DiscordBot.Configs;

namespace DiscordBot.Commands;

public class RefreshCommand : Command
{
	private readonly DiscordConfig _config;
	public static event Func<Task> OnRefreshed;
	
	public RefreshCommand(string? commandName, string? description, DiscordConfig config) : base(commandName, description)
	{
		_config = config;
	}

	public override async Task ExecuteAsync(SocketSlashCommand command)
	{
		await command.DeferAsync();
		await _config.SetWorkspaceNamesAsync();
		await command.RespondSuccessDelayed(command.User, "Workspaces Updated", string.Join("\n", _config.WorkspaceNames));
		await OnRefreshed.Invoke();
	}
}
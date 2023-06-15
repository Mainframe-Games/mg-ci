using Discord;
using Discord.WebSocket;
using DiscordConfig = DiscordBot.Configs.DiscordConfig;

namespace DiscordBot.Commands;

public class RefreshCommand : Command
{
	private readonly DiscordConfig _config;
	public static event Func<Task> OnRefreshed;
	
	public RefreshCommand(string? commandName, string? description, DiscordConfig config) : base(commandName, description)
	{
		_config = config;
	}

	public override async Task ExecuteAsync(SocketSlashCommand command, IUser user)
	{
		await command.DeferAsync();
		await _config.SetWorkspaceNamesAsync();
		await command.RespondSuccessDelayed(user, "Workspaces Updated", string.Join("\n", _config.WorkspaceNames));
		await OnRefreshed.Invoke();
	}
}
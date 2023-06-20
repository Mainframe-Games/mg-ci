using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using SharedLib;

namespace DiscordBot.Commands;

public class ClanforgeCommand : Command
{
	public override string? CommandName => "server-update-clanforge";
	public override string? Description => "Updates the clanforge game image";

	public override SlashCommandProperties Build()
	{
		var profile = new SlashCommandOptionBuilder()
			.WithName("profile")
			.WithDescription("Which profile to use")
			.WithType(ApplicationCommandOptionType.String)
			.WithRequired(true)
			.AddChoice("Production A", "proda")
			.AddChoice("Production B", "prodb")
			.AddChoice("Development A", "deva")
			.AddChoice("Development B", "devb");

		var beta = new SlashCommandOptionBuilder()
			.WithName("beta")
			.WithDescription("Which beta branch to use")
			.WithType(ApplicationCommandOptionType.String)
			.WithRequired(true)
			.AddChoice("Default", "default")
			.AddChoice("Development", "development")
			.AddChoice("Beta", "beta");

		var desc = new SlashCommandOptionBuilder()
			.WithName("description")
			.WithRequired(true)
			.WithDescription("Build Version: XXX")
			.WithType(ApplicationCommandOptionType.String);
		
		return CreateCommand()
			.AddOption(profile)
			.AddOption(beta)
			.AddOption(desc)
			.Build();
	}

	public override async Task ExecuteAsync(SocketSlashCommand command)
	{
		try
		{
			await command.DeferAsync();
			
			var profile = GetOptionValueString(command, "profile");
			var beta = GetOptionValueString(command, "beta");
			var description = GetOptionValueString(command, "description");

			var body = new JObject
			{
				["gameServerUpdate"] = new JObject
				{
					["clanforge"] = new JObject
					{
						["profile"] = profile,
						["beta"] = beta,
						["desc"] = description,
					}
				}
			};
			
			var res = await Web.SendAsync(HttpMethod.Post, DiscordWrapper.Config.BuildServerUrl, body: body);
			await command.RespondSuccessDelayed(command.User, "Game Server Update Started", res.Content);
		}
		catch (Exception e)
		{
			Logger.Log(e);
			await command.RespondErrorDelayed(command.User, "Build Server request failed", e.Message);
		}
	}
}
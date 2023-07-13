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
			.WithDescription("Version of the build to help differentiate each upload")
			.WithType(ApplicationCommandOptionType.String);
		
		return CreateCommand()
			.AddOption(profile)
			.AddOption(beta)
			.AddOption(desc)
			.Build();
	}

	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		try
		{
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
			return new CommandResponse("Game Server Update Started", res.Content);
		}
		catch (Exception e)
		{
			Logger.Log(e);
			return new CommandResponse("Build Server request failed", e.Message, true);
		}
	}
}
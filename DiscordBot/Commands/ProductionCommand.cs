using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using SharedLib;

namespace DiscordBot.Commands;

public class ProductionCommand : Command
{
	public override string? CommandName => "production";
	public override string? Description => "Puts steps in place for production release";

	public override SlashCommandProperties Build()
	{
		var password = new SlashCommandOptionBuilder()
			.WithName("password")
			.WithRequired(true)
			.WithDescription("Password is version of the game you intend to release")
			.WithType(ApplicationCommandOptionType.String);
		
		return CreateCommand()
			.AddOption(password)
			.Build();
	}

	public override async Task ExecuteAsync(SocketSlashCommand command)
	{
		try
		{
			var workspaceName = GetOptionValueString(command, "workspace");
			var password = GetOptionValueString(command, "password");
			await command.DeferAsync();
			
			// request to build server
			var body = new JObject
			{
				["productionProcess"] = new JObject
				{
					["workspaceName"] = workspaceName,
					["profile"] = "proda",
					["beta"] = "default",
					["password"] = password
				}
			};
			
			var res = await Web.SendAsync(HttpMethod.Post, DiscordWrapper.Config.BuildServerUrl, body: body);
			await command.RespondSuccessDelayed(command.User, "Production Process Started", res.Content);
		}
		catch (Exception e)
		{
			Logger.Log(e);
			await command.RespondErrorDelayed(command.User, "Build Server request failed", e.Message);
		}
	}

}
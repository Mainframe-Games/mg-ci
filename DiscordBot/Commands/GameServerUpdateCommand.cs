using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using SharedLib;

namespace DiscordBot.Commands;

/// <summary>
///  TODO: need to work out how to do interactions ... need a URL
/// </summary>
public class GameServerUpdateCommand : Command
{
	public override string? CommandName => "update-server-image";
	public override string? Description => "Requests to master server to update game server images";

	public override SlashCommandProperties Build()
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName("backend")
			.WithDescription("UGS or Clanforge backend")
			.WithType(ApplicationCommandOptionType.Integer)
			.AddChoice("usg", 0)
			.AddChoice("clanforge", 1);
		
		return CreateCommand()
			.AddOption(opt)
			.Build();
	}

	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		try
		{
			// TODO: Dynamic way of getting options

			var body = new JObject
			{
				["gameServerUpdate"] = new JObject
				{

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

	public override async Task ModifyOptions(SocketSlashCommand command)
	{
		// Get the value of the first option
		var firstOption = command.Data.Options.FirstOrDefault(x => x.Name == "backend");

		if (firstOption == null)
			return;

		// Modify the original response based on the first option's value
		var firstOptionValue = (int)(long)firstOption.Value;
		// var newOptionName = firstOptionValue == 0 ? "backend" : "";
		var newOptions = GetNewOptions(firstOptionValue); // Implement this method to return the dynamic options based on the first option's value

		// Create a new message component with the modified options
		var component = new ComponentBuilder()
			.WithSelectMenu("second_option", newOptions)
			.Build();

		// Modify the original response to update the options
		await command.ModifyOriginalResponseAsync(x =>
		{
			x.Content = "Options have been updated.";
			x.Components = new Optional<MessageComponent>(component);
		});
	}

	private static List<SelectMenuOptionBuilder> GetNewOptions(int optionIndex)
	{
		return optionIndex switch
		{
			// ugs
			0 => new List<SelectMenuOptionBuilder>
			{
			},
			
			// clanforge
			1 => new List<SelectMenuOptionBuilder>
			{
				new()
				{
					Label = "profile",
					Description = "Profile to update on clanforge"
				}
			},
			
			_ => throw new ArgumentException($"Index not recognised: {optionIndex}")
		};
	}
}
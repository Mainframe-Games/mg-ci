using System;
using DiscordBot;
using DiscordBot.Configs;

Console.Title = $"Discord Bot - {DiscordWrapper.Version}";
var config = DiscordConfig.Load();
var discord = new DiscordWrapper(config);
await discord.Init();

Console.WriteLine("Discord server stopped");
Console.Read();
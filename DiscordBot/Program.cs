using DiscordBot;
using DiscordBot.Configs;

var config = await DiscordConfig.LoadAsync();
if (config == null)
	throw new NullReferenceException("Config is null");
var discord = new DiscordWrapper(config);
await discord.Init();

Console.WriteLine("Discord server stopped");
Console.Read();
using System.CommandLine;
using MG_CLI;

var rootCommand = new RootCommand("Mainframe CI Tool")
{
    new Commit(),
    new DiscordHook(),
    
    new GodotCommands(),
    new ItchioCommands(),
    new SteamCommands(),
    
    new CsprojVersioning(),
    
    new DigitalOcean(),
};

if (args.Length == 0)
    Log.Logo();

// Invoke the command
var parseResult =  rootCommand.Parse(args);
return await parseResult.InvokeAsync();


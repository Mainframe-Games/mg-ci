using System.CommandLine;
using CLI.Commands;

var rootCommand = new RootCommand("Mainframe CI Tool")
{
    new TestCommand(),
    
    new Commit(),
    new DiscordHook(),
    
    new GodotBuild(),
    new GodotSetup(),
    new GodotVersioning(),
    
    new ItchioDeploy(),
    new SteamCmdSetup(),
    new SteamDeploy(),
    
};

// Invoke the command
var parseResult =  rootCommand.Parse(args);
return await parseResult.InvokeAsync();


using System.CommandLine;
using MG_CLI;

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
    
    new MixamoCommand(),
    
};

// Invoke the command
var parseResult =  rootCommand.Parse(args);
return await parseResult.InvokeAsync();


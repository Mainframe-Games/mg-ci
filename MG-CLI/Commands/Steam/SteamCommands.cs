using System.CommandLine;

namespace MG_CLI;

public class SteamCommands : Command
{
    public SteamCommands() : base("steam", "Manage Steam related tasks")
    {
        Add(new SteamCmdSetup());
        Add(new SteamDeploy());
    }
}
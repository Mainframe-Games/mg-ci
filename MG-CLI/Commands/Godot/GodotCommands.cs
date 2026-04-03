using System.CommandLine;

namespace MG_CLI;

public class GodotCommands : Command 
{
    public GodotCommands() : base("godot", "Commands for interacting with Godot projects and assets")
    {
        Add(new GodotImport());
        Add(new GodotBuild());
        Add(new GodotVersion());
        Add(new GodotInstall());
    }
}
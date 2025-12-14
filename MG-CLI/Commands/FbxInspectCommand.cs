using System.CommandLine;
using Spectre.Console;

namespace MG_CLI;

public class FbxInspectCommand : Command
{
    private readonly Option<string> _path = new("--path", "-p")
    {
        HelpName = "Path to FBX file",
    };
    
    public FbxInspectCommand() : base("fbx-inspect", "Inspects a FBX file and prints to file next to the FBX")
    {
        Add(_path);
        SetAction(Run);
    }

    private int Run(ParseResult arg)
    {
        var path = arg.GetValue(_path);

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            path = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose an FBX:")
                    .AddChoices(GetAllFbxFiles()));
        }
        
        FbxInspector.InspectFbx(path);
        return 0;
    }
    
    private static string[] GetAllFbxFiles()
    {
        return Directory.GetFiles(Environment.CurrentDirectory, "*.fbx", SearchOption.AllDirectories);
    }
}
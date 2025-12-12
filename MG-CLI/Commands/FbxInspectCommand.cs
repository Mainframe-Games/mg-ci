using System.CommandLine;

namespace MG_CLI;

public class FbxInspectCommand : Command
{
    private readonly Option<string> _path = new("--path", "-p")
    {
        HelpName = "Path to FBX file",
        Required = true,
    };
    
    public FbxInspectCommand() : base("fbx-inspect", "Inspects a FBX file and prints to file next to the FBX")
    {
        Add(_path);
        SetAction(Run);
    }

    private int Run(ParseResult arg)
    {
        var path = arg.GetRequiredValue(_path);
        FbxInspector.InspectFbx(path);
        return 0;
    }
}
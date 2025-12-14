using System.CommandLine;
using Spectre.Console;

namespace MG_CLI;

public class MixamoCommand : Command
{
    private readonly Option<string> _baseModel = new("--base")
    {
        HelpName = "Path to the base Mixamo-rigged FBX file.",
        Required = true,
    };

    private readonly Option<string> _animsDirOption = new("--anims")
    {
        HelpName = "Directory containing Mixamo animation FBX files.",
        Required = true
    };

    private readonly Option<string> _outGlbOption = new("--out")
    {
        HelpName = "Path to the output GLB file (e.g. ./out/character_with_anims.glb).",
        Required = true
    };
    
    public MixamoCommand() : base("mixamo", "Tools for working with Mixamo rigs and animations")
    {
        Add(_baseModel);
        Add(_animsDirOption);
        Add(_outGlbOption);
        SetAction(Run);
    }

    private int Run(ParseResult result)
    {
        try
        {
            var basePath = result.GetRequiredValue(_baseModel);
            var animDir = result.GetRequiredValue(_animsDirOption);
            var outFile = result.GetRequiredValue(_outGlbOption);
            
            // inspect 
            // FbxInspector.InspectFbx(basePath);
            
            // convert the FBX to GLB
            var model = FbxToGlbConverter.ExportFbxToGlb(basePath, outFile);
            // MixamoAnimations.AddAnimationsFromMixamoDir(model, animDir);
            
            // model.SaveGLB(outFile);
            
            Log.Print($"[mixamo] Exported to {outFile}", Color.Green);
        }
        catch (Exception e)
        {
            Log.Exception(e);
            return -1;
        }
        
        return 0;
    }
}

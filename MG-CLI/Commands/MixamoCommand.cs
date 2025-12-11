using System.CommandLine;

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
        var basePath = result.GetRequiredValue(_baseModel);
        var animDir = result.GetRequiredValue(_animsDirOption);
        var outFile = result.GetRequiredValue(_outGlbOption);

        var ok = MixamoTools.ValidateMixamoRig(basePath);
        if (!ok)
            return -1;
        
        MixamoTools.BuildCombinedGodotGlb(basePath, animDir, outFile);
        
        return 0;
    }
}

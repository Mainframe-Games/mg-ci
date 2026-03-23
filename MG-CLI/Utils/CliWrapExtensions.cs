using CliWrap;

namespace MG_CLI;

public static class CliWrapExtensions
{
    public static readonly PipeTarget StdErrorPipe = PipeTarget.ToDelegate(str =>
    {
        if (str.Trim().StartsWith("warning", StringComparison.InvariantCultureIgnoreCase))
            Log.PrintWarning(str);
        
        Log.PrintError(str);
    });
    
    public static Command WithCustomPipes(this Command command, string tag)
    {
        return command
            .WithStandardOutputPipe(PipeTarget.ToDelegate(str => 
            {
                Log.PrintMarkup($"[gray]{tag}[/]{str}");
            }))
            .WithStandardErrorPipe(StdErrorPipe);
    }
}
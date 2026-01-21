using CliWrap;
using Spectre.Console;

namespace MG_CLI;

public static class CliWrapExtensions
{
    public static readonly PipeTarget StdOutPutPipe = PipeTarget.ToDelegate(str => Log.Print(str));
    public static readonly PipeTarget StdErrorPipe = PipeTarget.ToDelegate(str =>
    {
        if (str.Trim().StartsWith("warning", StringComparison.InvariantCultureIgnoreCase))
            Log.PrintWarning(str);
        
        Log.PrintError(str);
    });
    
    public static Command WithCustomPipes(this Command command)
    {
        return command
            .WithStandardOutputPipe(StdOutPutPipe)
            .WithStandardErrorPipe(StdErrorPipe);
    }
}
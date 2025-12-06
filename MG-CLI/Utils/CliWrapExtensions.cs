using CliWrap;
using Spectre.Console;

namespace MG;

public static class CliWrapExtensions
{
    public static readonly PipeTarget StdOutPutPipe = PipeTarget.ToDelegate(str => Log.Print(str));
    public static readonly PipeTarget StdErrorPipe = PipeTarget.ToDelegate(Log.PrintError);
    
    public static Command WithCustomPipes(this Command command)
    {
        return command
            .WithStandardOutputPipe(StdOutPutPipe)
            .WithStandardErrorPipe(StdErrorPipe);
    }
}
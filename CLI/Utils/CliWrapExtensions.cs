using CliWrap;
using Spectre.Console;

namespace CLI.Utils;

public static class CliWrapExtensions
{
    private static readonly PipeTarget StdOutPutPipe = PipeTarget.ToDelegate(str => Log.WriteLine(str, Color.Default));
    private static readonly PipeTarget StdErrorPipe = PipeTarget.ToDelegate(err => Log.WriteLine(err, Color.Red));
    
    public static Command WithCustomPipes(this Command command)
    {
        return command
            .WithStandardOutputPipe(StdOutPutPipe)
            .WithStandardErrorPipe(StdErrorPipe);
    }
}
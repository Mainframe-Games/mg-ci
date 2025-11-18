using CliWrap;
using Spectre.Console;

namespace MG;

public static class CliWrapExtensions
{
    public static readonly PipeTarget StdOutPutPipe = PipeTarget.ToDelegate(str => Log.WriteLine(str, Color.Default));
    public static readonly PipeTarget StdErrorPipe = PipeTarget.ToDelegate(err => Log.WriteLine(err, Color.Red));
    
    public static Command WithCustomPipes(this Command command)
    {
        return command
            .WithStandardOutputPipe(StdOutPutPipe)
            .WithStandardErrorPipe(StdErrorPipe);
    }
}
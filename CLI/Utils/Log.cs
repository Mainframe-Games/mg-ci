using Spectre.Console;

namespace CLI.Utils;

public static class Log
{
    public static void WriteLine(string message, Color? color = null)
    {
        AnsiConsole.Write(new Text(message, color ?? Color.Default));
        AnsiConsole.WriteLine();
    }
    
    public static void Exception(Exception exception)
    {
        AnsiConsole.Write(new Text(exception.ToString(), Color.Red));
        AnsiConsole.WriteLine();
    }
}
using System.Text.RegularExpressions;
using Spectre.Console;

namespace MG;

public static partial class Log
{
    private static string? LogToFile { get; set; }
    public static bool IsLoggingToFile { get; private set; }
    
    public static void Print(string message, Color? color = null)
    {
        AnsiConsole.Write(new Text(message, color ?? Color.White));
        AnsiConsole.WriteLine();
        ToFile(message);
    }

    public static void PrintError(string message)
    {
        Print($"[ERROR] {message}", Color.Red);
    }

    public static void Exception(Exception exception)
    {
        AnsiConsole.Write(new Text(exception.ToString(), Color.Red));
        AnsiConsole.WriteLine();
    }

    public static void CreateLogFile(in string path)
    {
        if (File.Exists(path))
            File.Delete(path);
        
        LogToFile = path;
        var fullPath = Path.GetFullPath(LogToFile);
        var fileInfo = new FileInfo(fullPath);
        fileInfo.Directory?.Create();
        fileInfo.Create().Dispose();
        IsLoggingToFile = true;
        Print($"Created log file: {fullPath}", Color.Aqua);
    }
    
    public static void StopLoggingToFile()
    {
        IsLoggingToFile = false;
    }
    
    private static void ToFile(string message)
    {
        if (LogToFile is null || !IsLoggingToFile)
            return;
        
        var line = StripANSI(message);
        File.AppendAllText(LogToFile, line + Environment.NewLine);
    }

    private static string StripANSI(string input)
        => MyRegex().Replace(input, "");

    [GeneratedRegex(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
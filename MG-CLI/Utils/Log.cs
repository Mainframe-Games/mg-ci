using System.Text.RegularExpressions;
using Spectre.Console;

namespace MG_CLI;

public enum LogLevel : byte
{
    Debug,
    Info,
    Warning,
    Error
}

public static partial class Log
{
    private static string? LogToFile { get; set; }
    private static bool IsLoggingToFile { get; set; }
    private static LogLevel LogFileMinLogLevel { get; set; } = LogLevel.Debug; // the min level that will log to file
    private static StreamWriter? _logStreamWriter;
    
    private static void PrintInternal(in string message, in Color? color, in LogLevel level)
    {
        AnsiConsole.Write(new Text(message, color ?? Color.White));
        AnsiConsole.WriteLine();
        ToFile(message, level);
    }
    
    public static void Print(in string message)
    {
        PrintInternal(message, Color.White, LogLevel.Debug);
    }

    public static void Success(string message)
    {
        PrintInternal(message, Color.Green, LogLevel.Info);
    }
    
    public static void PrintWarning(string message)
    {
        PrintInternal(message, Color.Yellow, LogLevel.Warning);
    }

    public static void PrintError(string message)
    {
        PrintInternal(message, Color.Red, LogLevel.Error);
    }

    public static void CreateLogFile(in string path, in LogLevel minLogLevel)
    {
        if (File.Exists(path))
            File.Delete(path);

        LogToFile = path;
        var fullPath = Path.GetFullPath(LogToFile);
        var fileInfo = new FileInfo(fullPath);
        fileInfo.Directory?.Create();

        _logStreamWriter = new StreamWriter(fullPath, append: true) { AutoFlush = true };
        Print($"Created log file: {fullPath}");
        
        IsLoggingToFile = true;
        LogFileMinLogLevel = minLogLevel;
    }
    
    public static void StopLoggingToFile()
    {
        IsLoggingToFile = false;
        _logStreamWriter?.Flush();
        _logStreamWriter?.Dispose();
        _logStreamWriter = null;
    }
    
    private static void ToFile(string message, LogLevel level)
    {
        if (level < LogFileMinLogLevel)
            return;
        
        if (_logStreamWriter is null || !IsLoggingToFile)
            return;

        var line = StripANSI(message);
        _logStreamWriter.WriteLine(line);
    }

    private static string StripANSI(string input)
        => MyRegex().Replace(input, "");

    [GeneratedRegex(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
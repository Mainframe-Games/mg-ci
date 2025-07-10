using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SharedLib;

public static class Logger
{
    private static string TimeStamp => DateTime.Now.ToString("T");
    private static readonly StringBuilder _builder = new();

    public static void Log(object? message)
    {
        if (message == null || string.IsNullOrEmpty(message.ToString()))
            WriteLineInternal(string.Empty);
        else
            WriteLineInternal($"[{TimeStamp}] {message}");
    }

    public static void LogTitle(string title, params (string key, string value)[] logs)
    {
        var log = new StringBuilder();
        log.AppendLine(string.Empty);
        log.AppendLine("###########################");
        log.AppendLine($"{title}");
        log.AppendLine("###########################");

        foreach (var pair in logs)
            log.AppendLine($"{pair.key}: {pair.value}");

        WriteLineInternal(log.ToString());
    }

    private static void WriteLineInternal(string message)
    {
        _builder.AppendLine(message);
        Console.WriteLine(message);
    }

    public static void LogTimeStamp(
        string message,
        Stopwatch stopwatch,
        bool restartStopwatch = false
    )
    {
        var timeSpan = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
        Log($"{message} {timeSpan.ToHourMinSecString()}");

        if (restartStopwatch)
            stopwatch.Restart();
    }

    /// <summary>
    /// Writes all logs to file
    /// </summary>
    /// <param name="dirPath">Directory path to store log files</param>
    /// <param name="clearConsole"></param>
    public static void WriteToFile(string dirPath, bool clearConsole)
    {
        var logsDir = Path.Combine(dirPath, "Logs");
        Directory.CreateDirectory(logsDir);
        var logTimeStamp = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
        var path = Path.Combine(logsDir, $"{logTimeStamp}.log");
        File.WriteAllText(path, _builder.ToString());

        if (clearConsole)
            Clear();

        Console.WriteLine($"Log file written to: {new FileInfo(path).FullName}");
    }

    public static void Clear()
    {
        Console.Clear();
        _builder.Clear();
    }
}

using System.Diagnostics;
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

	private static void WriteLineInternal(string message)
	{
		_builder.AppendLine(message);
		Console.WriteLine(message);
	}

	public static void LogTimeStamp(string message, DateTime startTime)
	{
		Log($"{message} {(DateTime.Now - startTime).ToHourMinSecString()}");
	}
	
	public static void LogTimeStamp(string message, Stopwatch stopwatch)
	{
		var timeSpan = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
		Log($"{message} {timeSpan.ToHourMinSecString()}");
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
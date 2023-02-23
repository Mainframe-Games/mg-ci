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

	public static void WriteToFile(bool clearConsole)
	{
		Directory.CreateDirectory("Logs");
		var logTimeStamp = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
		var path = $"Logs/{logTimeStamp}.log";
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
namespace SharedLib;

public static class Logger
{
	private static string TimeStamp => DateTime.Now.ToString("T"); 
	
	public static void Log(object? message)
	{
		if (message == null || string.IsNullOrEmpty(message.ToString()))
			Console.WriteLine();
		else
			Console.WriteLine($"[{TimeStamp}] {message}");
	}
}
namespace SharedLib;

public static class Logger
{
	private static string TimeStamp => DateTime.Now.ToString("t"); 
	
	public static void Log(object message)
	{
		Console.WriteLine($"[{TimeStamp}] {message}");
	}
}
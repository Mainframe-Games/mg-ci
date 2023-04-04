using Server.Configs;
using SharedLib;

namespace Server;

public static class App
{
	private static string RootDirectory { get; set; }
	private static ListenServer? Server { get; set; }
	
	public static async Task RunAsync()
	{
		var config = ServerConfig.Load();
		RootDirectory = Environment.CurrentDirectory;
		
		Args.Environment.TryGetArg("-server", 0, out string ip, config.IP);
		Args.Environment.TryGetArg("-server", 1, out int port, config.Port);
		Server = new ListenServer(ip, (ushort)port);

		if (config.AuthTokens is { Count: > 0 })
		{
			Server.GetAuth = () =>
			{
				config.Refresh();
				return config.AuthTokens;
			};
		}
	
		// server should wait for ever
		await Server.RunAsync();
		Logger.Log("Server stopped");
	}

	public static void DumpLogs()
	{
		Logger.WriteToFile(RootDirectory, true);
		Server?.CheckIfServerStillListening();
		Environment.CurrentDirectory = RootDirectory; // reset cur dir back to root of exe
	}
}
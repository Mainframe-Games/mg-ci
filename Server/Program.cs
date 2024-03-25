using Server;
using SharedLib;
using WebSocketSharp.Server;

try
{
	Console.Title = $"Build Server - {App.Version}";
	
	var server = new WebSocketServer ("ws://localhost:8080");
	server.AddWebSocketService<TestService> ("/Test");
	server.Start ();
	
	Console.ReadKey (true);
	server.Stop ();
	
	// await App.RunAsync(new Args(args));
}
catch (Exception e)
{
	Logger.Log(e);
}

Console.WriteLine("---- End of program ----");
Console.Read();
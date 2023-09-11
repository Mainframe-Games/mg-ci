using Server;
using SharedLib;

try
{
	Console.Title = $"Build Server - {App.Version}";
	await App.RunAsync(new Args(args));
}
catch (Exception e)
{
	Logger.Log(e);
}

Console.WriteLine("---- End of program ----");
Console.Read();
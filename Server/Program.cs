// See https://aka.ms/new-console-template for more information

using Server;
using SharedLib;

try
{
	Console.Title = $"Build Server - {App.Version}";
	var argsObj = new Args(args);
	await App.RunAsync(argsObj);
}
catch (Exception e)
{
	Logger.Log(e);
}

Console.WriteLine("---- End of program ----");
Console.Read();
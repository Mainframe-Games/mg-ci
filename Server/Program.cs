// See https://aka.ms/new-console-template for more information

using Server;
using SharedLib;

try
{
	await App.RunAsync(args);
}
catch (Exception e)
{
	Logger.Log(e);
}

Console.WriteLine("---- End of program ----");
Console.Read();
using Deployment;
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
using Deployment;
using Deployment.Server;
using Deployment.Server.Config;
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
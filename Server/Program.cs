// See https://aka.ms/new-console-template for more information

using Server;
using SharedLib;

try
{
	Console.Title = $"Build Server - {App.Version}";
	
	var argsObj = new Args(args);

	if (argsObj.IsFlag("-test"))
	{
		await Web.StreamToServerAsync(
			"http://127.0.0.1:8080",
			@"C:\Users\Brogan\Desktop\Builds",
			0,
			Guid.NewGuid().ToString());
	}
	else
	{
		await App.RunAsync(argsObj);
	}
}
catch (Exception e)
{
	Logger.Log(e);
}

Console.WriteLine("---- End of program ----");
Console.Read();
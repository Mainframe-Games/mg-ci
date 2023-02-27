using Deployment;
using Deployment.Server;
using Deployment.Server.Config;
using SharedLib;

try
{
	var config = ServerConfig.Load();
	var originalDir = Environment.CurrentDirectory;

	if (config.RunServer || Args.Environment.IsFlag("-server", false))
	{
		Args.Environment.TryGetArg("-server", 0, out string ip, config.IP);
		Args.Environment.TryGetArg("-server", 1, out int port, config.Port);
		var server = new ListenServer(ip, (ushort)port);

		if (config.AuthTokens is { Count: > 0 })
		{
			server.GetAuth = () =>
			{
				config.Refresh();
				return config.AuthTokens;
			};
		}
		
		// when build pipeline completed dump logs and clear console
		BuildPipeline.OnCompleted += () =>
		{
			Logger.WriteToFile(originalDir, true);
			server.CheckIfServerStillListening();
		};
		
		// server should wait for ever
		await server.RunAsync();
		Logger.Log("Server stopped");
	}
	else
	{
		var currentWorkspace = Workspace.GetWorkspace();
		Logger.Log($"Chosen workspace: {currentWorkspace}");
		var pipe = new BuildPipeline(currentWorkspace, args);
		await pipe.RunAsync();
	}
}
catch (Exception e)
{
	Logger.Log(e);
}

Console.WriteLine("End of program");
Console.Read();
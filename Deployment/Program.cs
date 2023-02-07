using Deployment;
using Deployment.Server;
using SharedLib;

try
{
	var config = ServerConfig.Load();

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

Console.Read();
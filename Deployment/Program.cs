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
				return config.AuthTokens ?? Enumerable.Empty<string>();
			};
		}
		
		await server.RunAsync();
		Console.WriteLine("Server stopped");
	}
	else
	{
		var currentWorkspace = Workspace.GetWorkspace();
		Console.WriteLine($"Chosen workspace: {currentWorkspace}");
		var pipe = new BuildPipeline(currentWorkspace, args);
		await pipe.RunAsync();
	}
}
catch (Exception e)
{
	Console.WriteLine(e);
}

Console.Read();
using Deployment;
using Deployment.Server;

var config = ServerConfig.Load();

if (config.RunServer)
{
	var server = new ListenServer(config.IP, config.Port);
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
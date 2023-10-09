using System.Net;

namespace SharedLib.Server;

public interface IEndpoint
{
	string Path { get; }
	Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext);
}
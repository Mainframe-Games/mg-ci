using System.Net;

namespace SharedLib.Server;

public interface IServerCallbacks
{
	ListenServer Server { get; set; }
	Task<ServerResponse> Get(HttpListenerContext context);
	Task<ServerResponse> Post(HttpListenerContext context);
	Task<ServerResponse> Put(HttpListenerContext context);
}
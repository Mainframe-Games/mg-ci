using System.Net;
using Server;

namespace SharedLib.Server;

public interface IServerCallbacks
{
	public Endpoint? GetEndPoint(HttpListenerContext context);
}
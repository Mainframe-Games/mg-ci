using SharedLib.Server;

namespace Server.RemoteBuild;

public interface IRemoteControllable
{
	/// <summary>
	/// Returns response message
	/// </summary>
	/// <returns></returns>
	ServerResponse Process();
}
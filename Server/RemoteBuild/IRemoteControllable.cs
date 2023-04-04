namespace Server.RemoteBuild;

public interface IRemoteControllable
{
	/// <summary>
	/// Returns response message
	/// </summary>
	/// <returns></returns>
	string Process();
}
namespace SharedLib.Server;

/// <summary>
/// used for processing data on listen server
/// </summary>
public interface IProcessable
{
	/// <summary>
	/// Returns response message
	/// </summary>
	/// <returns></returns>
	ServerResponse Process();
}
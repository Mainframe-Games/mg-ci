namespace Deployment.RemoteBuild;

public interface IRemoteControllable
{
	Task<string> ProcessAsync();
}
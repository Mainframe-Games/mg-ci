using Deployment.Misc;
using Deployment.Server;
using Deployment.Server.Config;
using SharedLib;

namespace Deployment.Deployments;

/// <summary>
/// Unity's Cloud Content Delivery
/// <para></para>
/// Docs: https://docs.unity.com/ccd/UnityCCDCLI.html
/// </summary>
public class CcdDeploy
{
	private readonly CcdConfigServer _config;
	private readonly UnityServicesConfig _unityConfig;

	public CcdDeploy(CcdConfigServer config)
	{
		_config = config;
		_unityConfig = ServerConfig.Instance.Unity;
	}

	public void Deploy(string? pathToBuild)
	{
		SetProject();
		SetBucket();
		SyncData(pathToBuild);
	}

	private void SyncData(string? pathToBuild)
	{
		var (exitCode, output) = Cmd.Run(_config.PathToUcd, $"entries sync {pathToBuild} --environment={_unityConfig.EnvironmentId}");

		if (exitCode != 0)
			throw new Exception(output);
	}

	private void SetProject()
	{
		var (exitCode, output) = Cmd.Run(_config.PathToUcd, $"config set project {_unityConfig.ProjectId} --environment={_unityConfig.EnvironmentId}");

		if (exitCode != 0)
			throw new Exception(output);
	}

	private void SetBucket()
	{
		var (exitCode, output) = Cmd.Run(_config.PathToUcd, $"config set bucket {_config.BucketId}");

		if (exitCode != 0)
			throw new Exception(output);
	}
}

using System.Net;
using Deployment.Server;
using Deployment.Server.Config;
using SharedLib;

namespace Deployment.Deployments;

public class MultiplayDeploy
{
	private readonly UnityServicesConfig _unityConfig;

	public MultiplayDeploy()
	{
		_unityConfig = ServerConfig.Instance.Unity;
	}

	/// <summary>
	/// https://services.docs.unity.com/multiplay-config/v1/index.html#tag/Builds/operation/CreateNewBuildVersion
	/// </summary>
	/// <param name="pathToBuild"></param>
	/// <exception cref="Exception"></exception>
	public async Task Deploy(string pathToBuild)
	{
		UploadBuildToCCD(pathToBuild);
		await UpdateBuildVersion();
	}
	
	private void UploadBuildToCCD(string pathToBuild)
	{
		var ccdDeploy = new CcdDeploy(ServerConfig.Instance.Unity.Ccd); 
		ccdDeploy.Deploy(pathToBuild);
	}

	private async Task UpdateBuildVersion()
	{
		var body = new MultiplayPostBody
		{
			Ccd = new MultiplayPostBody.CcdBody
			{
				BucketId = _unityConfig.Ccd.BucketId,
			}
		};

		var url = $"https://services.api.unity.com/multiplay/builds/v1/projects/{_unityConfig.ProjectId}/environments/{_unityConfig.EnvironmentId}/builds/{_unityConfig.Multiplay.BuildId}/versions";
		var res = await Web.SendAsync(HttpMethod.Put, url, _unityConfig.Multiplay.AuthToken, body);

		if (res.StatusCode != HttpStatusCode.OK)
			throw new Exception($"Multiplay failed: {res.Reason}");
		
		Logger.Log(res.Content);
	}

	public class MultiplayPostBody
	{
		public CcdBody Ccd { get; set; }
		public ContainerBody Container { get; set; }
		public bool ForceRollout { get; set; }
		
		public class CcdBody
		{
			public string BucketId { get; set; }
		}
		
		public class ContainerBody
		{
			public string ImageTag { get; set; }
		}
	}
}
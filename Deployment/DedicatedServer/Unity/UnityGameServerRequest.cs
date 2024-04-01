using Newtonsoft.Json.Linq;
using SharedLib;

namespace Deployment.Server.Unity;

public class UnityGameServerRequest : UnityWebRequest
{
	public UnityGameServerRequest(string keyId, string secretKey) : base(keyId, secretKey)
	{
	}
	
	/// <summary>
	/// Docs: https://services.docs.unity.com/multiplay-config/v1/index.html#tag/Builds/operation/CreateNewBuildVersion
	/// </summary>
	/// <param name="projectId"></param>
	/// <param name="environmentId"></param>
	/// <param name="buildId"></param>
	/// <param name="s3Url"></param>
	/// <param name="s3AccessKey"></param>
	/// <param name="s3SecretKey"></param>
	/// <param name="forceRollout"></param>
	/// <returns></returns>
	public async Task CreateNewBuildVersion(
		string projectId,
		string environmentId,
		ulong buildId,
		string s3Url, 
		string s3AccessKey,
		string s3SecretKey,
		bool forceRollout = false)
	{
		// refresh on unity's end
		var url =
			$"https://services.api.unity.com/multiplay/builds/v1" +
			$"/projects/{projectId}" +
			$"/environments/{environmentId}" +
			$"/builds/{buildId}" +
			$"/versions";

		var body = new JObject
		{
			["forceRollout"] = forceRollout,
			["s3"] = new JObject
			{
				["s3URI"] = s3Url,
				["accessKey"] = s3AccessKey,
				["secretKey"] = s3SecretKey,
			}
		};

		await Web.SendAsync(HttpMethod.Post, url, AuthToken, body);
	}
}
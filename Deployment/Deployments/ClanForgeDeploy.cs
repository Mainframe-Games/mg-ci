using System.Net;
using System.Text;
using Deployment.Server.Config;
using Newtonsoft.Json.Linq;
using SharedLib;

namespace Deployment.Deployments;

/// <summary>
/// Docs: https://docs.unity.com/game-server-hosting/manual/legacy/update-your-game-via-the-clanforge-api#Managed_Game_Server_Hosting_(Clanforge)_API
/// </summary>
public class ClanForgeDeploy
{
	private const string BASE_URL = "https://api.multiplay.co.uk/cfp/v1";
	private const int POLL_TIME = 5000;
	
	/// <summary>
	/// A base64 encoded string of '{AccessKey}:{SecretKey}'
	/// </summary>
	private string AuthToken { get; }
	private uint ASID { get; }
	private uint MachineId { get; }
	private uint[] ImageIds { get; }
	private string Desc { get; }
	private string Url { get; }

	public ClanForgeDeploy(ClanforgeConfig? clanforgeConfig, string? desc)
	{
		if (clanforgeConfig == null)
			throw new NullReferenceException($"Param {nameof(clanforgeConfig)} can not be null");

		AuthToken = $"Basic {Base64Key.Generate(clanforgeConfig.AccessKey, clanforgeConfig.SecretKey)}";
		ASID = clanforgeConfig.Asid;
		MachineId = clanforgeConfig.MachineId;
		ImageIds = clanforgeConfig.ImageIds ?? Array.Empty<uint>();
		Url = Uri.EscapeDataString(clanforgeConfig.Url ?? string.Empty);
		Desc = desc ?? string.Empty;
	}

	public async Task Deploy()
	{
		var tasks = ImageIds.Select(DeployInternal);
		await Task.WhenAll(tasks);
	}

	/// <summary>
	/// Entry build for deploying build to clanforge.
	/// Must be done after Steam Deploy
	/// </summary>
	private async Task DeployInternal(uint imageId)
	{
		// create image
		Logger.Log("...creating new image");
		var updateId = await CreateNewImage(imageId);

		// poll for when diff is ready
		Logger.Log("...polling image status");
		await PollStatus("imageupdate", $"updateid={updateId}");

		// generate diff
		Logger.Log("...generating diff");
		var diffId = await GenerateDiff(imageId);

		// poll for diff status
		Logger.Log("...polling diff status");
		await PollStatus("imagediff", $"diffid={diffId}");
		
		// accept diff and create new image version
		await CreateImageVersion(diffId);
	}

	#region Requests

	/// <summary>
	/// Docs: https://docs.unity.com/game-server-hosting/en/manual/api/endpoints/image-update-create
	/// </summary>
	/// <returns>updateid</returns>
	/// <exception cref="WebException"></exception>
	private async Task<int> CreateNewImage(uint imageId)
	{
		var url = $"{BASE_URL}/imageupdate/create?imageid={imageId}&desc=\"{Desc}\"&machineid={MachineId}&accountserviceid={ASID}&url={Url}";
		var content = await SendRequest(url);
		return content["updateid"]?.Value<int>() ?? -1;
	}

	/// <summary>
	/// Docs: https://docs.unity.com/game-server-hosting/en/manual/api/endpoints/image-update-status
	/// </summary>
	/// <returns>success</returns>
	/// <exception cref="WebException"></exception>
	private async Task PollStatus(string path, string paramStr)
	{
		var url = $"{BASE_URL}/{path}/status?accountserviceid={ASID}&{paramStr}";
		var isCompleted = false;

		while (!isCompleted)
		{
			var content = await SendRequest(url);
			var stateName = content["jobstatename"]?.ToString();
			Console.WriteLine($"...{path} status: {stateName}");
			isCompleted = stateName == "Completed";

			if (isCompleted)
				ThrowIfNotSuccess(content);
			else
				await Task.Delay(POLL_TIME);
		}
	}

	/// <summary>
	/// Docs: https://docs.unity.com/game-server-hosting/en/manual/api/endpoints/image-diff-create
	/// </summary>
	/// <returns>diffid</returns>
	private async Task<int> GenerateDiff(uint imageId)
	{
		var url = $"{BASE_URL}/imagediff/create?imageid={imageId}&machineid={MachineId}&accountserviceid={ASID}";
		var content = await SendRequest(url);
		return content["diffid"]?.Value<int>() ?? -1;
	}

	/// <summary>
	/// Docs: docs.unity.com/game-server-hosting/en/manual/api/endpoints/image-create-version
	/// </summary>
	private async Task CreateImageVersion(int diffId)
	{
		var url = $"{BASE_URL}/imageversion/create?diffid={diffId}&accountserviceid={ASID}&restart=0&force=0&full=1&game_build=\"{Desc}\"";
		var content = await SendRequest(url);
		ThrowIfNotSuccess(content);
	}
	
	/// <summary>
	/// Helper wrapper for sending web requests with auth token and header with error throwing
	/// </summary>
	/// <param name="url"></param>
	/// <returns></returns>
	/// <exception cref="WebException"></exception>
	private async Task<JObject> SendRequest(string url)
	{
		var res = await Web.SendAsync(HttpMethod.Get, url, AuthToken, headers: (HttpRequestHeader.ContentType, "application/x-www-form-urlencoded"));
		var content = JObject.Parse(res.Content);
		ThrowIfError(content);
		return content;
	}
	
	private static void ThrowIfNotSuccess(JObject content)
	{
		if (content["success"]?.Value<bool>() == false)
			throw new WebException($"Status failed. Please check ClanForge dashboard for more information. {content}");
	}
	
	private static void ThrowIfError(JObject content)
	{
		if (content["error"]?.Value<bool>() == true)
			throw new WebException(content["error_message"]?.ToString());
	}
	
	#endregion
}
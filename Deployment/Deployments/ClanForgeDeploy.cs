using System.Diagnostics;
using System.Net;
using Deployment.Configs;
using Newtonsoft.Json.Linq;
using SharedLib;

namespace Deployment.Deployments;

/// <summary>
/// Docs: https://docs.unity.com/game-server-hosting/manual/legacy/update-your-game-via-the-clanforge-api#Managed_Game_Server_Hosting_(Clanforge)_API
/// </summary>
public class ClanForgeDeploy
{
	private const string BASE_URL = "https://api.multiplay.co.uk/cfp/v1";
	private const int POLL_TIME_MS = 10000;
	
	/// <summary>
	/// A base64 encoded string of '{AccessKey}:{SecretKey}'
	/// </summary>
	private string AuthToken { get; }
	private uint ASID { get; }
	private uint MachineId { get; }
	private uint ImageId { get; }
	private bool Full { get; }
	private string Desc { get; }
	private string Url { get; }

	public ClanForgeDeploy(ClanforgeConfig? clanforgeConfig, string? profile, string? desc, string? beta, bool? full)
	{
		if (clanforgeConfig == null)
			throw new NullReferenceException($"Param {nameof(clanforgeConfig)} can not be null");

		AuthToken = $"Basic {Base64Key.Generate(clanforgeConfig.AccessKey, clanforgeConfig.SecretKey)}";
		ASID = clanforgeConfig.Asid;
		MachineId = clanforgeConfig.MachineId;
		ImageId = clanforgeConfig.GetImageId(profile);
		Url = Uri.EscapeDataString(clanforgeConfig.GetUrl(beta));
		Desc = desc ?? string.Empty;
		Full = full ?? false;
	}

	/// <summary>
	/// Entry build for deploying build to clanforge.
	/// Must be done after Steam Deploy
	/// </summary>
	public async Task Deploy()
	{
		var sw = Stopwatch.StartNew();
		var startTime = DateTime.Now;
		
		// create image
		Logger.Log("...creating new image");
		var updateId = await CreateNewImage(ImageId);
		Logger.LogTimeStamp("Image Created: ", sw, true);

		// poll for when diff is ready
		Logger.Log("...polling image status");
		await PollStatus("imageupdate", $"updateid={updateId}");
		Logger.LogTimeStamp("Image Updated Polling: ", sw, true);

		// generate diff
		Logger.Log("...generating diff");
		var diffId = await GenerateDiff(ImageId);
		Logger.LogTimeStamp("Generated Diff: ", sw, true);

		// poll for diff status
		Logger.Log("...polling diff status");
		await PollStatus("imagediff", $"diffid={diffId}");
		Logger.LogTimeStamp("Generated Diff Polling: ", sw, true);

		// accept diff and create new image version
		await CreateImageVersion(diffId);
		Logger.LogTimeStamp("Image Version Created: ", sw, true);
		Logger.LogTimeStamp("Clanforge Deployed: ", startTime);
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
			Logger.Log($"...{path} status: {stateName}");
			isCompleted = stateName is "Completed" or "Failed";

			if (isCompleted)
				ThrowIfNotSuccess(content);
			else
				await Task.Delay(POLL_TIME_MS);
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
	/// Docs: https://docs.unity.com/game-server-hosting/en/manual/api/endpoints/image-create-version
	/// </summary>
	private async Task CreateImageVersion(int diffId)
	{
		var fullNum = Full ? "1" : "0";
		var url = $"{BASE_URL}/imageversion/create?diffid={diffId}&accountserviceid={ASID}&restart=0&force=1&full={fullNum}&game_build=\"{Desc}\"";
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
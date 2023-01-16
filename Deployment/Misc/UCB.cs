using System.IO.Compression;
using System.Net;
using Newtonsoft.Json.Linq;
using SharedLib;

namespace Deployment.Misc;

public class UCB
{
	private readonly string authToken;
	private readonly string orgid;
	private readonly string projectid;

	public UCB(string apiKey, string orgid, string projectid)
	{
		this.authToken = $"Basic {apiKey}";
		this.orgid = orgid;
		this.projectid = projectid;
	}

	/// <summary>
	/// Returns directory path
	/// </summary>
	/// <param name="target"></param>
	/// <param name="seconds"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public async Task<string> BuildAndDownloadTarget(string target, int seconds = 60)
	{
		var buildNumber = await StartBuild(target);
		await Task.Delay(TimeSpan.FromSeconds(5 * 60));
		
		var (status, downloadLink) = await PollForCompletion(target, buildNumber, seconds);

		if (status is not "success")
			throw new Exception($"Build failure: {status}");

		return await DownloadFile(downloadLink, $"{Environment.CurrentDirectory}/Builds/{target}.zip");
	}

	/// <summary>
	/// Starts a new build on Unity Cloud Build
	/// </summary>
	/// <param name="buildtargetid"></param>
	/// <param name="cleanBuild"></param>
	/// <param name="commitHash"></param>
	/// <returns>Returns build number</returns>
	private async Task<int> StartBuild(string buildtargetid, bool cleanBuild = false, string commitHash = null)
	{
		// curl
		// 	-X POST
		// 	-d '{"clean": true, "delay": 30}'
		// 	-H "Content-Type: application/json"
		// 	-H "Authorization: Basic [YOUR API KEY]"
		// https://build-api.cloud.unity3d.com/api/v1/orgs/{orgid}/projects/{projectid}/buildtargets/{buildtargetid}/builds

		var res = await Web.SendAsync(
			HttpMethod.Post,
			$"https://build-api.cloud.unity3d.com/api/v1/orgs/{orgid}/projects/{projectid}/buildtargets/{buildtargetid}/builds",
			authToken,
			new JObject { ["clean"] = cleanBuild, ["commit"] = commitHash },
			headers: (HttpRequestHeader.ContentType.ToString(), "application/json"));

		var json = JObject.Parse(res.Content);

		if (json["error"] != null)
			throw new WebException(json["error"]?.ToString());
		
		return json["build"]?.Value<int>() ?? -1;
	}

	private async Task<(string status, string downloadLink)> PollForCompletion(string buildtargetid, int number, int seconds)
	{
		/*
		 * curl
		  -X GET
		  -H "Content-Type: application/json"
		  -H "Authorization: Basic [YOUR API KEY]"
		  https://build-api.cloud.unity3d.com/api/v1/orgs/{orgid}/projects/{projectid}/buildtargets/{buildtargetid}/builds/{number}
		 */

		while (true)
		{
			var res = await Web.SendAsync(
				HttpMethod.Get,
				$"https://build-api.cloud.unity3d.com/api/v1/orgs/{orgid}/projects/{projectid}/buildtargets/{buildtargetid}/builds/{number}",
				authToken);
			
			var json = JObject.Parse(res.Content);

			var status = json["buildStatus"].ToString();
			Console.WriteLine($"{buildtargetid} {number}: {status}");

			switch (status)
			{
				case "success":
					var downloadLink = json.SelectToken("links.download_primary.href", true).ToString();
					return (status, downloadLink);
				
				case "cancelled" or "failed":
					return (status, null);

				default:
					await Task.Delay(TimeSpan.FromSeconds(seconds));
					break;
			}
		}
	}

	private static async Task<string> DownloadFile(string url, string destination)
	{
		Console.WriteLine($"Downloading file: {url}");
	
		var file = new FileInfo(destination);
		if (!file.Directory?.Exists ?? false)
			file.Directory.Create();

		var client = new HttpClient();
		client.Timeout = TimeSpan.FromMinutes(10);
		using var response = await client.GetAsync(url);
		await using var stream = await response.Content.ReadAsStreamAsync();
		await using var zip = File.OpenWrite(destination);
		await stream.CopyToAsync(zip);

		var dirPath = destination.Replace(".zip", "");
		ZipFile.ExtractToDirectory(destination, dirPath);
		DeleteDoNotShips(dirPath);
		
		return dirPath;
	}

	private static void DeleteDoNotShips(string dirPath)
	{
		var dirs = new DirectoryInfo(dirPath).GetDirectories();
		foreach (var directory in dirs)
		{
			if (directory.Name.Contains("_BurstDebugInformation_DoNotShip"))
				directory.Delete(true);
		}
	}
}
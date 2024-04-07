using System.Diagnostics;
using System.Net;
using ClanforgeDeployment.Utils;
using Newtonsoft.Json.Linq;

namespace ClanforgeDeployment;

/// <summary>
/// Docs: https://docs.unity.com/game-server-hosting/manual/legacy/update-your-game-via-the-clanforge-api#Managed_Game_Server_Hosting_(Clanforge)_API
/// </summary>
/// <param name="authToken">A base64 encoded string of '{AccessKey}:{SecretKey}'</param>
/// <param name="asid"></param>
/// <param name="machineId"></param>
/// <param name="imageId"></param>
/// <param name="full"></param>
/// <param name="desc"></param>
/// <param name="url"></param>
public class ClanForgeDeploy(
    string authToken,
    uint asid,
    uint machineId,
    uint imageId,
    bool full,
    string desc,
    string url
)
{
    private const string BASE_URL = "https://api.multiplay.co.uk/cfp/v1";
    private const int POLL_TIME_MS = 10000;

    // public ClanForgeDeploy(
    // )
    // {
    //     if (clanforgeConfig == null)
    //         throw new NullReferenceException($"Param {nameof(clanforgeConfig)} can not be null");
    //
    //     AuthToken =
    //         $"Basic {Base64Key.Generate(clanforgeConfig.AccessKey, clanforgeConfig.SecretKey)}";
    //     ASID = clanforgeConfig.Asid;
    //     MachineId = clanforgeConfig.MachineId;
    //     ImageId = clanforgeConfig.GetImageId(profile);
    //     Url = Uri.EscapeDataString(clanforgeConfig.GetUrl(beta));
    //     Desc = desc ?? string.Empty;
    //     Full = full ?? false;
    // }

    /// <summary>
    /// Entry build for deploying build to clanforge.
    /// Must be done after Steam Deploy
    /// </summary>
    public async Task Deploy()
    {
        var sw = Stopwatch.StartNew();
        var startTime = DateTime.Now;

        // create image
        Console.WriteLine("...creating new image");
        var updateId = await CreateNewImage(imageId);
        // Logger.LogTimeStamp("Image Created: ", sw, true);

        // poll for when diff is ready
        Console.WriteLine("...polling image status");
        await PollStatus("imageupdate", $"updateid={updateId}");
        // Logger.LogTimeStamp("Image Updated Polling: ", sw, true);

        // generate diff
        Console.WriteLine("...generating diff");
        var diffId = await GenerateDiff(imageId);
        // Logger.LogTimeStamp("Generated Diff: ", sw, true);

        // poll for diff status
        Console.WriteLine("...polling diff status");
        await PollStatus("imagediff", $"diffid={diffId}");
        // Logger.LogTimeStamp("Generated Diff Polling: ", sw, true);

        // accept diff and create new image version
        await CreateImageVersion(diffId);
        // Logger.LogTimeStamp("Image Version Created: ", sw, true);
        // Logger.LogTimeStamp("Clanforge Deployed: ", startTime);
    }

    #region Requests

    /// <summary>
    /// Docs: https://docs.unity.com/game-server-hosting/en/manual/api/endpoints/image-update-create
    /// </summary>
    /// <returns>updateid</returns>
    /// <exception cref="WebException"></exception>
    private async Task<int> CreateNewImage(uint imageId)
    {
        var _url =
            $"{BASE_URL}/imageupdate/create?imageid={imageId}&desc=\"{desc}\"&machineid={machineId}&accountserviceid={asid}&url={url}";
        var content = await SendRequest(_url);
        return content["updateid"]?.Value<int>() ?? -1;
    }

    /// <summary>
    /// Docs: https://docs.unity.com/game-server-hosting/en/manual/api/endpoints/image-update-status
    /// </summary>
    /// <returns>success</returns>
    /// <exception cref="WebException"></exception>
    private async Task PollStatus(string path, string paramStr)
    {
        var url = $"{BASE_URL}/{path}/status?accountserviceid={asid}&{paramStr}";
        var isCompleted = false;

        while (!isCompleted)
        {
            var content = await SendRequest(url);
            var stateName = content["jobstatename"]?.ToString();
            Console.WriteLine($"...{path} status: {stateName}");
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
        var url =
            $"{BASE_URL}/imagediff/create?imageid={imageId}&machineid={machineId}&accountserviceid={asid}";
        var content = await SendRequest(url);
        return content["diffid"]?.Value<int>() ?? -1;
    }

    /// <summary>
    /// Docs: https://docs.unity.com/game-server-hosting/en/manual/api/endpoints/image-create-version
    /// </summary>
    private async Task CreateImageVersion(int diffId)
    {
        var fullNum = full ? "1" : "0";
        var url =
            $"{BASE_URL}/imageversion/create?diffid={diffId}&accountserviceid={asid}&restart=0&force=1&full={fullNum}&game_build=\"{desc}\"";
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
        var res = await Web.SendAsync(
            HttpMethod.Get,
            url,
            authToken,
            headers: (HttpRequestHeader.ContentType, "application/x-www-form-urlencoded")
        );
        var content = JObject.Parse(res);
        ThrowIfError(content);
        return content;
    }

    private static void ThrowIfNotSuccess(JObject content)
    {
        if (content["success"]?.Value<bool>() == false)
            throw new WebException(
                $"Status failed. Please check ClanForge dashboard for more information. {content}"
            );
    }

    private static void ThrowIfError(JObject content)
    {
        if (content["error"]?.Value<bool>() == true)
            throw new WebException(content["error_message"]?.ToString());
    }

    #endregion
}

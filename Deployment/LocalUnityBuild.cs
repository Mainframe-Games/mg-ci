using System.Net;
using Deployment.Configs;
using Deployment.Misc;
using Deployment.RemoteBuild;
using Deployment.Server;
using Newtonsoft.Json.Linq;
using SharedLib;

namespace Deployment;

public enum UnityTarget
{
	None,
	Win64,
	OSXUniversal,
	Linux64
}

public class LocalUnityBuild
{
	private const string DEFAULT_EXECUTE_METHOD = "BuildSystem.BuildScript.BuildPlayer";
	private readonly string _unityVersion;

	/// <summary>
	/// build ids we are waiting for
	/// </summary>
	private readonly List<string> _buildIds = new();

	public LocalUnityBuild(string unityVersion)
	{
		_unityVersion = unityVersion;
	}
	
	private string GetDefaultUnityPath(string? versionExtension)
	{
		return OperatingSystem.IsMacOS()
			? $"/Applications/Unity/Hub/Editor/{_unityVersion}{versionExtension}/Unity.app/Contents/MacOS/Unity"
			: $@"C:\Program Files\Unity\Hub\Editor\{_unityVersion}{versionExtension}\Editor\Unity.exe";
	}

	/// <summary>
	/// Builds the player
	/// </summary>
	/// <param name="targetConfig"></param>
	/// <returns>Directory of build</returns>
	public async Task<bool> Build(TargetConfig targetConfig)
	{
		var logPath = $"{targetConfig.BuildPath}.log";
		var buildStartTime = DateTime.Now;
		
		var exePath = GetDefaultUnityPath(targetConfig.VersionExtension);
		var executeMethod = targetConfig.ExecuteMethod ?? DEFAULT_EXECUTE_METHOD;
		
		Logger.Log(string.Empty);
		Logger.Log($"Starting build '{targetConfig.Target}': {DateTime.Now:g}");
		var (exitCode, output) = Cmd.Run(exePath, $"-quit -batchmode -buildTarget {targetConfig.Target} " +
		                                          $"-projectPath . -executeMethod {executeMethod} " +
		                                          $"-logFile {logPath} -settings {targetConfig.Settings}");

		if (exitCode != 0)
			throw new Exception($"Build failed. Read log file: {Path.Combine(Environment.CurrentDirectory, logPath)}");
		
		var buildTime = DateTime.Now - buildStartTime;
		Logger.Log($"Build Success! Build Time: {buildTime:hh\\:mm\\:ss}");
		await Task.Delay(10);
		return true;
	}

	/// <summary>
	/// Called from main build server. Sends web request to offload server and gets a buildId in return
	/// </summary>
	/// <param name="workspaceName"></param>
	/// <param name="targetConfig"></param>
	/// <param name="offloadUrl">The url to the offload server</param>
	/// <param name="sendBackUrl">The url to send back the build response</param>
	/// <returns>True is request is successful. Not if build is successful</returns>
	/// <exception cref="WebException"></exception>
	public async Task<bool> SendRemoteBuildRequest(string? workspaceName, TargetConfig targetConfig, string? offloadUrl, string? sendBackUrl)
	{
		var remoteBuild = new RemoteBuildTargetRequest
		{
			WorkspaceName = workspaceName,
			Config = targetConfig,
			SendBackUrl = sendBackUrl
		};
		
		var body = new RemoteBuildPacket { BuildTargetRequest = remoteBuild };
		var res = await Web.SendAsync(HttpMethod.Post, offloadUrl, DeviceInfo.UniqueDeviceId, body);

		if (res.StatusCode != HttpStatusCode.OK)
			throw new WebException(res.Reason);

		var json = JObject.Parse(res.Content);
		var buildId = json.SelectToken("data", true)?.ToString();
		Logger.Log($"Remote build id: {buildId}");
		_buildIds.Add(buildId);
		return true;
	}

	public async Task RemoteBuildReceived(RemoteBuildResponse remoteBuildResponse)
	{
		var buildId = remoteBuildResponse.BuildId;
		
		if (!_buildIds.Contains(buildId))
			throw new Exception($"Build ID not expected: {buildId}");

		var zip = $"{remoteBuildResponse.Request.Config.BuildPath}.zip";
		var base64 = remoteBuildResponse.Base64;
		var buildPath = remoteBuildResponse.Request.Config.BuildPath;
		await FilePacker.UnpackAsync(zip, base64, buildPath);
		_buildIds.Remove(buildId);
	}

	/// <summary>
	/// Returns once buildIds count is 0
	/// </summary>
	public async Task WaitBuildIds()
	{
		while (_buildIds.Count > 0)
		{
			Console.Write($"Remaining buildIds: ({_buildIds.Count}) {string.Join(",\n", _buildIds)}");
			await Task.Delay(2000);
		}
	}
}
using System.Net;
using Deployment.Configs;
using Deployment.Misc;
using Deployment.RemoteBuild;
using Deployment.Server;
using Deployment.Server.Config;
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
	private readonly string? _unityVersion;

	/// <summary>
	/// build ids we are waiting for
	/// </summary>
	private readonly List<string> _buildIds = new();
	
	public string? Errors { get; private set; }

	public LocalUnityBuild(string? unityVersion)
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
		var errorPath = $"{targetConfig.BuildPath}_errors.log";
		
		if (File.Exists(errorPath))
			File.Delete(errorPath);
		
		var buildStartTime = DateTime.Now;
		
		var exePath = GetDefaultUnityPath(targetConfig.VersionExtension);
		var executeMethod = targetConfig.ExecuteMethod ?? DEFAULT_EXECUTE_METHOD;
		
		Logger.Log(string.Empty);
		Logger.Log($"Starting build: {targetConfig.Target}");

		var cliparams = BuildCliParams(targetConfig, executeMethod, logPath);
		var (exitCode, output) = Cmd.Run(exePath, cliparams);

		if (exitCode != 0)
		{
			if (File.Exists(errorPath))
			{
				Errors = await File.ReadAllTextAsync(errorPath);
				Logger.Log($"Build Failed with code '{exitCode}'\n{Errors}");
			}
			else
			{
				Logger.Log($"Build Failed with code '{exitCode}'");
			}
			
			Logger.Log($"Verbose log file: {Path.Combine(Environment.CurrentDirectory, logPath)}");
			return false;
		}
		
		var buildTime = DateTime.Now - buildStartTime;
		Logger.Log($"Build Success! {targetConfig.Target}, Build Time: {buildTime:hh\\:mm\\:ss}");
		await Task.Delay(10);
		return true;
	}

	private static string BuildCliParams(TargetConfig targetConfig, string executeMethod, string logPath)
	{
		var cliparams = new List<string>
		{
			"-quit",
			"-batchmode",
			$"-buildTarget {targetConfig.Target}",
			"-projectPath .",
			$"-executeMethod {executeMethod}",
			$"-logFile {logPath}",
			$"-settings {targetConfig.Settings}"
		};

		// for server builds
		var isServerBuild = targetConfig.Settings?.ToLower().Contains("server") == true;
		var subTarget = isServerBuild ? "Server" : "Player";
		cliparams.Add($"-standaloneBuildSubtarget {subTarget}");

		return string.Join(" ", cliparams);
	}

	/// <summary>
	/// Called from main build server. Sends web request to offload server and gets a buildId in return
	/// </summary>
	/// <param name="workspaceName"></param>
	/// <param name="changeSetId"></param>
	/// <param name="buildVersion"></param>
	/// <param name="targetConfig"></param>
	/// <param name="offloadUrl">The url to the offload server</param>
	/// <returns>True is request is successful. Not if build is successful</returns>
	/// <exception cref="WebException"></exception>
	public async Task<bool> SendRemoteBuildRequest(string? workspaceName, int changeSetId, string? buildVersion, TargetConfig targetConfig, string? offloadUrl, bool cleanBuild)
	{
		if (offloadUrl == null)
			throw new Exception("OffloadUrl is null");
		
		var remoteBuild = new RemoteBuildTargetRequest
		{
			WorkspaceName = workspaceName,
			ChangeSetId = changeSetId,
			BuildVersion = buildVersion,
			Config = targetConfig,
			CleanBuild = cleanBuild,
			SendBackUrl = $"http://{ServerConfig.Instance.IP}:{ServerConfig.Instance.Port}"
		};
		
		var body = new RemoteBuildPacket { BuildTargetRequest = remoteBuild };
		var res = await Web.SendAsync(HttpMethod.Post, offloadUrl, DeviceInfo.UniqueDeviceId, body);

		if (res.StatusCode != HttpStatusCode.OK)
		{
			Logger.Log($"Web Request Failed '{res.StatusCode}' {res.Reason}");
			return false;
		}

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
		var cachedCount = -1;
		
		while (_buildIds.Count > 0)
		{
			// to limit the amount of log spamming just log when count changes
			if (_buildIds.Count != cachedCount)
			{
				Logger.Log($"Remaining buildIds: ({_buildIds.Count}) {string.Join(", ", _buildIds)}");
				cachedCount = _buildIds.Count;
			}
			
			await Task.Delay(3000);
		}
	}
}
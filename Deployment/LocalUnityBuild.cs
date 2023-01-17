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
	private readonly string _unityVersion;

	/// <summary>
	/// build ids we are waiting for
	/// </summary>
	private readonly List<string> _buildIds = new();

	public LocalUnityBuild(string unityVersion)
	{
		_unityVersion = unityVersion;
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
		
		var exePath = targetConfig.GetUnityPath(_unityVersion);
		var executeMethod = targetConfig.GetExecuteMethod();
		
		Console.WriteLine(string.Empty);
		Console.WriteLine($"Starting build '{targetConfig.Target}': {DateTime.Now:g}");
		var (exitCode, output) = Cmd.Run(exePath, $"-quit -batchmode -buildTarget {targetConfig.Target} " +
		                                          $"-projectPath . -executeMethod {executeMethod} " +
		                                          $"-logFile {logPath} -settings {targetConfig.Settings}");

		if (exitCode != 0)
			throw new Exception($"Build failed. Read log file: {Path.Combine(Environment.CurrentDirectory, logPath)}");
		
		var buildTime = DateTime.Now - buildStartTime;
		Console.WriteLine($"Build Success! Build Time: {buildTime:hh\\:mm\\:ss}");
		await Task.Delay(10);
		return true;
	}

	/// <summary>
	/// Called from main build server. Sends web request to offload server and gets a buildId in return
	/// </summary>
	/// <param name="workspace"></param>
	/// <param name="targetConfig"></param>
	/// <param name="offloadUrl"></param>
	/// <returns>True is request is successful. Not if build is successful</returns>
	/// <exception cref="WebException"></exception>
	public async Task<bool> SendRemoteBuildRequest(Workspace workspace, TargetConfig targetConfig, string offloadUrl)
	{
		var remoteBuild = new RemoteBuildTargetRequest
		{
			Workspace = workspace,
			Config = targetConfig
		};
		
		var body = new RemoteBuildPacket { BuildTargetRequest = remoteBuild };
		var res = await Web.SendAsync(HttpMethod.Post, offloadUrl, DeviceInfo.UniqueDeviceId, body);

		if (res.StatusCode != HttpStatusCode.OK)
			throw new WebException(res.Reason);

		var json = JObject.Parse(res.Content);
		var buildId = json.SelectToken("data", true)?.ToString();
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
			await Task.Delay(500);
	}
}
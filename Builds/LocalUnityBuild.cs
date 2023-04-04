using Deployment.Configs;
using SharedLib;

namespace Deployment;

public class LocalUnityBuild
{
	private const string DEFAULT_EXECUTE_METHOD = "BuildSystem.BuildScript.BuildPlayer";
	private readonly string? _unityVersion;

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
	/// <param name="projectPath">Full path to project folder as not to rely on relative paths</param>
	/// <param name="targetConfig"></param>
	/// <returns>Directory of build</returns>
	public void Build(string projectPath, TargetConfig targetConfig)
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

		var cliparams = BuildCliParams(targetConfig, projectPath, executeMethod, logPath);
		var (exitCode, output) = Cmd.Run(exePath, cliparams);

		if (exitCode != 0)
		{
			var verboseLog = $"Verbose log file: {Path.Combine(Environment.CurrentDirectory, logPath)}";
			
			if (File.Exists(errorPath))
			{
				Errors = File.ReadAllText(errorPath);
				throw new Exception($"Build Failed with code '{exitCode}'\n{Errors}\n{verboseLog}");
			}

			throw new Exception($"Build Failed with code '{exitCode}'\n{verboseLog}");
		}
		
		var buildTime = DateTime.Now - buildStartTime;
		Logger.Log($"Build Success! {targetConfig.Target}, Build Time: {buildTime:hh\\:mm\\:ss}");
	}

	private static string BuildCliParams(TargetConfig targetConfig, string projectPath, string executeMethod, string logPath)
	{
		var cliparams = new List<string>
		{
			"-quit",
			"-batchmode",
			$"-buildTarget {targetConfig.Target}",
			$"-projectPath \"{projectPath}\"",
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
}
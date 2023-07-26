using System.Text;
using Deployment.Configs;
using SharedLib;

namespace Deployment;

public class LocalUnityBuild
{
	private const string DEFAULT_EXECUTE_METHOD = "BuildSystem.BuildScript.BuildPlayer";

	private readonly Workspace _workspace;
	private readonly string? _unityVersion;

	public string? Errors { get; private set; }

	public LocalUnityBuild(Workspace workspace)
	{
		_workspace = workspace;
		_unityVersion = workspace.UnityVersion;
	}
	
	private string GetDefaultUnityPath(TargetConfig? config)
	{
		if (OperatingSystem.IsWindows())
			return $@"C:\Program Files\Unity\Hub\Editor\{_unityVersion}\Editor\Unity.exe";

		var isLinux = config?.Target is UnityTarget.Linux64;
		var isIL2CPP = isLinux && _workspace.IsIL2CPP(UnityTarget.Standalone.ToString());
		var x86_64 = isIL2CPP ? "-x86_64" : string.Empty;
		
		return OperatingSystem.IsMacOS()
			? $"/Applications/Unity/Hub/Editor/{_unityVersion}{x86_64}/Unity.app/Contents/MacOS/Unity"
			: $@"C:\Program Files\Unity\Hub\Editor\{_unityVersion}\Editor\Unity.exe";
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
		var buildReport = $"{targetConfig.BuildPath}_build_report.log";
		
		// delete error logs file
		if (File.Exists(errorPath))
			File.Delete(errorPath);
		
		var buildStartTime = DateTime.Now;
		var exePath = GetDefaultUnityPath(targetConfig);
		var executeMethod = targetConfig.ExecuteMethod ?? DEFAULT_EXECUTE_METHOD;

		Logger.Log($"Started Build: {targetConfig.Settings}");
		
		var cliparams = BuildCliParams(targetConfig, projectPath, executeMethod, logPath);
		var (exitCode, output) = Cmd.Run(exePath, cliparams);

		if (exitCode != 0)
		{
			var verboseLog = $"Verbose log file: {Path.Combine(Environment.CurrentDirectory, logPath)}";

			if (!string.IsNullOrEmpty(output))
				verboseLog += $"\nRAW OUTPUT: {output}";
			
			if (File.Exists(errorPath))
			{
				Errors = File.ReadAllText(errorPath);
				throw new Exception($"Build Failed with code '{exitCode}'\n{Errors}\n{verboseLog}");
			}

			throw new Exception($"Build Failed with code '{exitCode}'\n{verboseLog}");
		}
		
		Logger.LogTimeStamp($"Build Success! {targetConfig.Settings}, Build Time: ", buildStartTime);
		WriteBuildReport(logPath, buildReport);
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
			$"-logFile \"{logPath}\"",
			$"-settings {targetConfig.Settings}",
			$"-buildPath \"{targetConfig.BuildPath}\""
		};

		// for server builds
		var isServerBuild = targetConfig.Settings?.ToLower().Contains("server") == true;
		var subTarget = isServerBuild ? "Server" : "Player";
		cliparams.Add($"-standaloneBuildSubtarget {subTarget}");

		return string.Join(" ", cliparams);
	}
	
	private static void WriteBuildReport(string filePath, string outputPath)
	{
		var lines = File.ReadAllLines(filePath);
		var started = false;
		var report = new StringBuilder(); 
	
		foreach (var line in lines)
		{
			if (!started && line == "Build Report")
				started = true;
			else if (started && line.Contains("----"))
				break;

			if (started)
				report.AppendLine(line);
		}
	
		File.WriteAllText(outputPath, report.ToString());
	}
	
	public static void __TEST__()
	{
		const string UNITY_VERSION = "2021.3.25f1";
		const string PATH = "../../../../Unity/BuildTest";
		
		var dir = new DirectoryInfo(PATH);
		
		if (!dir.Exists)
			throw new DirectoryNotFoundException(dir.FullName);
		
		var target = new TargetConfig
		{
			Target = UnityTarget.Win64,
			Settings = "BuildSettings_Win64",
			BuildPath = "Builds/win64",
		};
		
		// var unity = new LocalUnityBuild(UNITY_VERSION);
		// unity.Build(dir.FullName, target);
	}
}
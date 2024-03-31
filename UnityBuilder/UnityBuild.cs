using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Deployment.Configs;
using SharedLib;
using SharedLib.Server;
using UnityBuilder;

namespace Deployment;

public class UnityBuild
{
    private const string DEFAULT_EXECUTE_METHOD = "BuildSystem.BuildScript.BuildPlayer";

    private readonly Workspace _workspace;
    private readonly string _projectPath;
    private readonly string _unityVersion;

    public UnityBuild(Workspace workspace)
    {
        _workspace = workspace;
        _projectPath = workspace.Directory ?? string.Empty;
        _unityVersion = workspace.UnityVersion ?? string.Empty;

        if (string.IsNullOrEmpty(_projectPath))
            throw new NullReferenceException($"{nameof(_projectPath)} can not be null or empty");
        if (string.IsNullOrEmpty(_unityVersion))
            throw new NullReferenceException($"{nameof(_unityVersion)} can not be null or empty");
    }

    /// <summary>
    /// Builds the player
    /// </summary>
    /// <param name="asset"></param>
    /// <returns>Directory of build</returns>
    public BuildResult Build(BuildSettingsAsset asset)
    {
        var buildPath = asset.BuildPath;
        var logPath = $"{buildPath}.log";
        var errorPath = $"{buildPath}_errors.log";
        var buildReport = $"{buildPath}_build_report.log";
        var sw = Stopwatch.StartNew();

        // delete error logs file
        if (File.Exists(errorPath))
            File.Delete(errorPath);

        var useIntel =
            asset.GetBuildTargetFlag() is BuildTargetFlag.Linux64
            && _workspace.IsIL2CPP(asset.TargetGroup);

        var exePath = UnityPath.GetDefaultUnityPath(_unityVersion, useIntel);

        Logger.Log($"Started Build: {asset.Name}");

        var cliparams = BuildCliParams(asset, _projectPath, DEFAULT_EXECUTE_METHOD, logPath);
        var (exitCode, output) = Cmd.Run(exePath, cliparams);

        sw.Stop();

        if (exitCode == 0)
        {
            Logger.LogTimeStamp($"Build Success! {asset.Name}, Build Time: ", sw);
            WriteBuildReport(logPath, buildReport);

            return new BuildResult
            {
                BuildName = asset.Name,
                BuildSize = new DirectoryInfo(asset.BuildPath).GetByteSize(),
                BuildTime = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds),
            };
        }

        // collect errors
        var errorMessage = new StringBuilder(output);
        if (File.Exists(errorPath))
            errorMessage.AppendLine(File.ReadAllText(errorPath));

        var verboseLogPath =
            $"Verbose log file: {Path.Combine(Environment.CurrentDirectory, logPath)}";
        Logger.Log($"Build Failed with code '{exitCode}'\n{verboseLogPath}");

        errorMessage.AppendLine(verboseLogPath);

        return new BuildResult
        {
            BuildName = asset.Name,
            BuildTime = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds),
            Errors = new ErrorResponse
            {
                Code = HttpStatusCode.InternalServerError,
                Exception = $"Unity Build Error. exitCode: {exitCode}",
                Message = errorMessage.ToString(),
                StackTrace = ErrorResponse.ParseStackTrace(Environment.StackTrace)
            }
        };
    }

    /// <summary>
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="projectPath"></param>
    /// <param name="executeMethod"></param>
    /// <param name="logPath"></param>
    /// <returns></returns>
    private static string BuildCliParams(
        BuildSettingsAsset asset,
        string projectPath,
        string executeMethod,
        string logPath
    )
    {
        var cliparams = new[]
        {
            "-quit",
            "-batchmode",
            $"-buildTarget {asset.GetBuildTargetFlag()}",
            $"-projectPath \"{projectPath}\"",
            $"-executeMethod \"{executeMethod}\"",
            $"-logFile \"{logPath}\"",
            $"-settings \"{asset.FileName}\"",
            $"-buildPath \"{asset.BuildPath}\"",
            $"-standaloneBuildSubtarget \"{asset.SubTarget}\""
        };

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
}

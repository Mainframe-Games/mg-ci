using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mainframe.CI.Runtime;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mainframe.CI.Editor
{
    public static class BuildScript
    {
        private static readonly string[] _args = Environment.GetCommandLineArgs();

        /// <summary>
        /// Called from build server
        /// </summary>
        public static void BuildPlayer()
        {
            Console.WriteLine($"BuildPlayer called with args: {string.Join(", ", _args)}");

            WriteAppVersion();

            var options = GetBuildOptions();
            BuildPlayer(options);
        }

        private static void WriteAppVersion()
        {
            try
            {
                var fullVersion = $"{Application.version}.{PlayerSettings.macOS.buildNumber}";
                var file = new FileInfo(AppVersion.FilePath);
                file.Directory?.Create();
                File.WriteAllText(file.FullName, fullVersion);
            }
            catch (Exception)
            {
                ExitWithResult(BuildResult.Failed);
            }
        }

        private static void BuildPlayer(BuildPlayerOptions options)
        {
            // TODO: set android keystore 
            // SetAndroidKeystore(settings);

            if (!EnsureBuildDirectoryExists(options))
            {
                ExitWithResult(BuildResult.Failed);
                return;
            }

            PrintBuildOptions(options);
            var report = BuildPipeline.BuildPlayer(options);

            PrintReportSummary(report.summary);
            if (IsFlag("-quit"))
                ExitWithResult(report.summary.result);
        }

        private static BuildPlayerOptions GetBuildOptions()
        {
            var scenes = GetScenePaths();

            var extraScriptingDefines = TryGetArg("-extraScriptingDefines", out var outExtraScriptingDefines)
                ? outExtraScriptingDefines.Split(',')
                : null;

            var locationPathName = GetDefaultBuildPath();

            TryGetArg("-assetBundleManifestPath", out var assetBundleManifestPath);

            var options = TryGetArg("-options", out var optionsStr)
                          && int.TryParse(optionsStr, out var optionsInt)
                ? optionsInt
                : 0;

            var buildOptions = new BuildPlayerOptions
            {
                target = EditorUserBuildSettings.activeBuildTarget,
                subtarget = (int)EditorUserBuildSettings.standaloneBuildSubtarget,
                options = (BuildOptions)options,
                targetGroup = GetActiveBuildTargetGroup(),
                scenes = scenes,
                locationPathName = locationPathName,
                extraScriptingDefines = extraScriptingDefines,
                assetBundleManifestPath = assetBundleManifestPath,
            };

            return buildOptions;
        }

        private static string GetDefaultBuildPath()
        {
            var extension = GetExtension();
            var path = Path.Combine("Builds",
                $"{Application.productName}_{EditorUserBuildSettings.activeBuildTarget}",
                Application.productName + extension);
            return path;
        }

        private static string GetExtension()
        {
            return EditorUserBuildSettings.activeBuildTarget switch
            {
                BuildTarget.StandaloneOSX => ".app",
                BuildTarget.StandaloneWindows64 => ".exe",
                BuildTarget.StandaloneWindows => ".exe",
                BuildTarget.WebGL => string.Empty,
                BuildTarget.iOS => string.Empty,
                BuildTarget.Android => ".aab",
                BuildTarget.EmbeddedLinux => ".86_64",
                BuildTarget.StandaloneLinux64 => ".86_64",
                _ => throw new ArgumentOutOfRangeException(nameof(EditorUserBuildSettings.activeBuildTarget),
                    $"buildTarget not supported: {EditorUserBuildSettings.activeBuildTarget}")
            };
        }

        private static BuildTargetGroup GetActiveBuildTargetGroup()
        {
            var prop = typeof(EditorUserBuildSettings)
                .GetProperty("activeBuildTargetGroup", BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null);

            var targetGroup = (BuildTargetGroup)(prop ?? BuildTargetGroup.Standalone);
            return targetGroup;
        }

        private static string[] GetScenePaths()
        {
            string[] scenePaths = null;

            if (TryGetArg("-scenes", out var scenesArg))
            {
                var sceneNames = scenesArg.Split(',');
                scenePaths = AssetDatabase.FindAssets("t:Scene", sceneNames)
                    .Select(AssetDatabase.AssetPathToGUID)
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray();
            }

            if (scenePaths?.Length > 0)
                return scenePaths;

            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(s => s.path)
                .ToArray();
        }

        private static void ExitWithResult(BuildResult result)
        {
            switch (result)
            {
                case BuildResult.Succeeded:
                    Console.WriteLine("Build succeeded!");
                    EditorApplication.Exit(0);
                    break;
                case BuildResult.Failed:
                    Console.WriteLine("Build failed!");
                    EditorApplication.Exit(101);
                    break;
                case BuildResult.Cancelled:
                    Console.WriteLine("Build cancelled!");
                    EditorApplication.Exit(102);
                    break;
                case BuildResult.Unknown:
                default:
                    Console.WriteLine("Build result is unknown!");
                    EditorApplication.Exit(103);
                    break;
            }
        }

        private static void PrintReportSummary(BuildSummary summary)
        {
            var report = new StringBuilder();
            report.AppendLine("###########################");
            report.AppendLine("#      Build results      #");
            report.AppendLine("###########################");
            report.AppendLine($"Duration: {summary.totalTime.ToString()}");
            report.AppendLine($"Warnings: {summary.totalWarnings.ToString()}");
            report.AppendLine($"Errors: {summary.totalErrors.ToString()}");
            report.AppendLine($"Size: {summary.totalSize.ToString()} bytes");
            report.AppendLine("###########################");
            Console.WriteLine(report.ToString());
        }

        private static void PrintBuildOptions(BuildPlayerOptions buildOptions)
        {
            var opts = new StringBuilder();

            opts.AppendLine("###########################");
            opts.AppendLine("#   Build Player Options  #");
            opts.AppendLine("###########################");
            opts.AppendLine($"target: {buildOptions.target}");
            opts.AppendLine($"targetGroup: {buildOptions.targetGroup}");
            opts.AppendLine($"subtarget: {buildOptions.subtarget}");
            opts.AppendLine($"location path: {buildOptions.locationPathName}");
            opts.AppendLine($"scenes: {string.Join(", ", buildOptions.scenes)}");

            if (buildOptions.options != 0)
                opts.AppendLine($"options: {buildOptions.options}");

            if (!string.IsNullOrEmpty(buildOptions.assetBundleManifestPath))
                opts.AppendLine($"assetBundleManifestPath: {buildOptions.assetBundleManifestPath}");

            if (buildOptions.extraScriptingDefines is not null)
                opts.AppendLine(
                    $"extraScriptingDefines: {string.Join(", ", buildOptions.extraScriptingDefines)}");

            opts.AppendLine("###########################");
            Console.WriteLine(opts.ToString());
        }

        private static bool EnsureBuildDirectoryExists(BuildPlayerOptions options)
        {
            if (string.IsNullOrEmpty(options.locationPathName))
                return true;

            var fullDir = new FileInfo(options.locationPathName).Directory;

            // ensure build target folder exits
            if (fullDir == null)
                throw new NullReferenceException($"Directory is null: {options.locationPathName}");

            if (!fullDir.Exists)
                fullDir.Create();

            return true;
        }

        private static void SetAndroidKeystore(string keyStorePath, string keyStoreAlias, string keyStorePassword)
        {
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore = true;

            PlayerSettings.Android.keystoreName = keyStorePath;
            PlayerSettings.Android.keystorePass = keyStorePassword;
            PlayerSettings.Android.keyaliasName = keyStoreAlias;
            PlayerSettings.Android.keyaliasPass = keyStorePassword;
        }

        public static bool IsFlag(string flag)
        {
            var index = Array.IndexOf(_args, flag);
            return index != -1 && index + 1 < _args.Length;
        }

        private static bool TryGetArg(string arg, out string value)
        {
            value = null;

            var index = Array.IndexOf(_args, arg);
            if (index == -1 || index + 1 >= _args.Length)
                return false;

            value = _args[index + 1];
            return true;
        }

        private static string GetArg(string arg)
        {
            var index = Array.IndexOf(_args, arg);
            if (index == -1 || index + 1 >= _args.Length)
                return null;
            return _args[index + 1];
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildSystem
{
    public static class BuildScript
    {
        private static readonly string Eol = Environment.NewLine;
        private static readonly StringBuilder _builder = new();
        
        private static readonly JsonSerializerSettings _settings =
            new()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = { new StringEnumConverter(), }
            };

        private static BuildPlayerOptions GetBuildOptions()
        {
            var optionsPath = Path.Combine(".ci", "build_options.json");
            var optionsJson = File.ReadAllText(optionsPath);
            var playerBuildOptions = JsonConvert.DeserializeObject<BuildPlayerOptions>(optionsJson, _settings);
            Console.WriteLine($"Build options: {playerBuildOptions}");

            if (playerBuildOptions.scenes.Length == 0)
                playerBuildOptions.scenes = BuildSettings.GetEditorSettingsScenes();
            
            return playerBuildOptions;
        }

        /// <summary>
        /// Called from build server
        /// </summary>
        public static void BuildPlayer()
        {
            var args = Environment.GetCommandLineArgs();
            Console.WriteLine("BuildPlayer called with args: " + string.Join(", ", args));

            var options = GetBuildOptions();
            BuildPlayer(options);
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

            Application.logMessageReceived += OnLogReceived;
            PrintBuildOptions(options);
            var report = BuildPipeline.BuildPlayer(options);
            Application.logMessageReceived -= OnLogReceived;

            PrintReportSummary(report.summary);
            DumpErrorLog(report);
            ExitWithResult(report.summary.result, report);
        }

        private static void ExitWithResult(BuildResult result, BuildReport report = null)
        {
            switch (result)
            {
                case BuildResult.Succeeded:
                    Console.WriteLine("Build succeeded!");
                    EditorApplication.Exit(0);
                    break;
                case BuildResult.Failed:
                    if (report != null)
                    {
                        var errors = report.steps
                            .SelectMany(x => x.messages)
                            .Where(x => x.type is LogType.Error or LogType.Exception or LogType.Assert)
                            .Select(x => $"[{x.type.ToString().ToUpper()}] {x.content}")
                            .Reverse()
                            .ToArray();

                        Console.WriteLine(string.Join("\n", errors), LogType.Error);
                    }
                    else
                    {
                        Console.WriteLine("Build failed!");
                    }

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
            Console.WriteLine(
                $"###########################{Eol}" +
                $"#      Build results      #{Eol}" +
                $"###########################{Eol}" +
                $"{Eol}" +
                $"Duration: {summary.totalTime.ToString()}{Eol}" +
                $"Warnings: {summary.totalWarnings.ToString()}{Eol}" +
                $"Errors: {summary.totalErrors.ToString()}{Eol}" +
                $"Size: {summary.totalSize.ToString()} bytes{Eol}" +
                $"{Eol}"
            );
        }

        private static void PrintBuildOptions(BuildPlayerOptions buildOptions)
        {
            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters = { new StringEnumConverter() }
            };

            Console.WriteLine(
                $"{Eol}" +
                $"###########################{Eol}" +
                $"#   Build Player Options  #{Eol}" +
                $"###########################{Eol}" +
                $"{Eol}" +
                $"{JsonConvert.SerializeObject(buildOptions, jsonSettings)}" +
                $"{Eol}"
            );
        }

        private static bool EnsureBuildDirectoryExists(BuildPlayerOptions options)
        {
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

        private static void DumpErrorLog(BuildReport report)
        {
            if (report.summary.totalErrors == 0)
                return;

            Console.WriteLine($"Build Failed is {report.summary.totalErrors} errors...\n{_builder}");

            var logFile = GetArgValue("-logFile");

            if (string.IsNullOrEmpty(logFile))
                return;

            var errorFileName = logFile.Replace(".log", "_errors.log");
            File.WriteAllText(errorFileName, _builder.ToString());
        }

        private static void OnLogReceived(string condition, string stacktrace, LogType type)
        {
            if (type is LogType.Log or LogType.Warning)
                return;

            _builder.AppendLine($"[{type.ToString().ToUpper()}] {condition}");

            if (!string.IsNullOrEmpty(stacktrace))
                _builder.AppendLine(stacktrace);
        }

        private static string GetArgValue(string arg)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == arg)
                    return args[i + 1];
            }

            return null;
        }
    }
}
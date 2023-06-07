using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildSystem
{
	public static class BuildScript
	{
		private static readonly string Eol = Environment.NewLine;
		private static readonly StringBuilder _builder = new();

		/// <summary>
		/// Called from build server
		/// </summary>
		public static void BuildPlayer()
		{
			var settingsArg = GetArgValue("-settings");
			var settings = GetBuildConfig(settingsArg);
			BuildPlayer(settings);
		}
		
		public static void BuildPlayer(BuildSettings settings)
		{
			if (!settings.IsValid())
				throw new Exception($"BuildSettings '{settings.name}' not valid");
			
			var buildPathRoot = GetArgValue("-buildPath");
			var options = settings.GetBuildOptions(buildPathRoot);
			
			RunPrebuild(settings, options);
			SetAndroidKeystore(settings);

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
					Log("Build succeeded!");
					Exit(0);
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
						
						Log(string.Join("\n", errors), LogType.Error);
					}
					else
					{
						Log("Build failed!", LogType.Error);
					}

					Exit(101);
					break;
				case BuildResult.Cancelled:
					Log("Build cancelled!", LogType.Warning);
					Exit(102);
					break;
				case BuildResult.Unknown:
				default:
					Log("Build result is unknown!", LogType.Error);
					Exit(103);
					break;
			}
			
			void Exit(int code)
			{
				if (Application.isBatchMode)
					EditorApplication.Exit(code);
			}
		}

		private static void PrintReportSummary(BuildSummary summary)
		{
			Log(
				$"{Eol}" +
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
		
			Log(
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

		/// <summary>
		/// Finds all classes that implement <see cref="IPrebuildProcess"/> and runs the callback method
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="options"></param>
		private static void RunPrebuild(BuildSettings settings, BuildPlayerOptions options)
		{
			// delete files
			var fileInfo = new FileInfo(options.locationPathName);
			if (settings.DeleteFiles && (fileInfo.Directory?.Exists ?? false))
				fileInfo.Directory.Delete(true);
			
			// pre-build interface search
			var types = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(s => s.GetTypes())
				.Where(p => !p.IsInterface && !p.IsAbstract && typeof(IPrebuildProcess).IsAssignableFrom(p))
				.ToArray();
			
			foreach (var type in types)
			{
				try
				{
					var inst = (IPrebuildProcess)Activator.CreateInstance(type);
					inst.OnPrebuildProcess(settings);
				}
				catch (Exception e)
				{
					// log exception
					Log(e, LogType.Exception);
					ExitWithResult(BuildResult.Failed);
					break;
				}
			}
		}

		private static void SetAndroidKeystore(BuildSettings settings)
		{
			if (settings.Target != BuildTarget.Android) 
				return;

			EditorUserBuildSettings.buildAppBundle = true;
			EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
			EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
			PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
			PlayerSettings.Android.useCustomKeystore = true;
			PlayerSettings.Android.keystoreName = settings.KeystorePath;
			PlayerSettings.Android.keystorePass = settings.KeystorePassword;
			PlayerSettings.Android.keyaliasName = settings.KeystoreAlias;
			PlayerSettings.Android.keyaliasPass = settings.KeystorePassword;
		}

		private static void DumpErrorLog(BuildReport report)
		{
			if (report.summary.totalErrors == 0)
				return;
			
			Log($"Build Failed is {report.summary.totalErrors} errors...\n{_builder}");
			
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

		private static BuildSettings GetBuildConfig(string buildSettingsName)
		{
			var guids = AssetDatabase.FindAssets($"t:{nameof(BuildSettings)}");
			
			for (int i = 0; i < guids.Length; i++)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);
				var config = AssetDatabase.LoadAssetAtPath<BuildSettings>(path);
				if (config.name.Equals(buildSettingsName, StringComparison.OrdinalIgnoreCase))
					return config;
			}

			throw new Exception($"Build settings not found '{buildSettingsName}'");
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

		private static void Log(object log, LogType logType = LogType.Log)
		{
			if (Application.isBatchMode)
			{
				Console.WriteLine(log);
				return;
			}

			switch (logType)
			{
				case LogType.Error:
					Debug.LogError(log);
					break;
				case LogType.Assert:
					Debug.LogAssertion(log);
					break;
				case LogType.Warning:
					Debug.LogWarning(log);
					break;
				case LogType.Log:
					Debug.Log(log);
					break;
				case LogType.Exception:
					Debug.LogException(log as Exception);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
			}
		}
	}
}
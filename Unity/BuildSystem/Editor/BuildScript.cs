using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildSystem
{
	public static class BuildScript
	{
		private static readonly StringBuilder _builder = new();

		private static void Exit(int code)
		{
			if (Application.isBatchMode)
				EditorApplication.Exit(code);
		}
		
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
			
			if (!EnsureBuildDirectoryExists(options))
			{
				Exit(555);
				return;
			}
			
			Debug.Log($"Started build: {options.locationPathName}");
			Application.logMessageReceived += OnLogReceived;

			if (settings.BuildAddressables)
				BuildAddressables();

			var report = BuildPipeline.BuildPlayer(options);
			
			Application.logMessageReceived -= OnLogReceived;
			
			if (report.summary.result == BuildResult.Succeeded)
			{
				Debug.Log($"Build Succeeded: {report.summary.outputPath}");
			}
			else
			{
				DumpErrorLog(report);
				Exit(666);
			}
		}

		private static void BuildAddressables()
		{
			AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
			AddressableAssetSettings.BuildPlayerContent();
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
					Debug.LogException(e);
					Exit(555);
					break;
				}
			}
		}

		private static void DumpErrorLog(BuildReport report)
		{
			Debug.LogError($"Build Failed is {report.summary.totalErrors} errors...\n{_builder}");
			
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
				if (config.name == buildSettingsName)
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
	}
}
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;

namespace BuildSystem
{
	public static class BuildScript
	{
		private static readonly StringBuilder _builder = new();

		/// <summary>
		/// Called from build server
		/// </summary>
		public static void BuildPlayer()
		{
			var settingsArg = GetArgValue("-settings");
			var ettings = GetBuildConfig(settingsArg);
			BuildPlayer(ettings);
		}
		
		public static void BuildPlayer(BuildSettings settings)
		{
			if (!settings.IsValid())
				throw new Exception($"BuildSettings '{settings.name}' not valid");

			Application.logMessageReceived += OnLogReceived;
			
			var options = settings.GetBuildOptions();
			var report = BuildPipeline.BuildPlayer(options);

			Application.logMessageReceived -= OnLogReceived;
			
			if (report.summary.result == BuildResult.Succeeded)
			{
				Debug.Log($"Build {report.summary.result}: {report.summary.outputPath}");
				CleanUp(settings.LocationPath);
			}
			else
			{
				Debug.LogError($"Build Failed is {report.summary.totalErrors} errors...\n{_builder}");
				var logFile = GetArgValue("-logFile");
				var errorFileName = logFile.Replace(".log", "_errors.log");
				File.WriteAllText(errorFileName, _builder.ToString());
				
				if (Application.isBatchMode)
					EditorApplication.Exit(666);
			}
		}

		private static void OnLogReceived(string condition, string stacktrace, LogType type)
		{
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
		
		private static void CleanUp(string buildDir)
		{
			var dirs = new DirectoryInfo(buildDir).GetDirectories();
			foreach (var dir in dirs)
			{
				if (dir.Name.Contains("_DoNotShip") || dir.Name.Contains("_ButDontShipItWithYourGame"))
					dir.Delete(true);
			}
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

		public class Response
		{
			public string Data { get; set; }
		}

		private static void SendReport()
		{
			var body = new Response { Data = _builder.ToString() };
			var json = JsonUtility.ToJson(body);
			var urlArg = GetArgValue("-serverUrl");
			var url = new Uri(urlArg);
			var req = UnityWebRequest.Post(url, json);
			req.SetRequestHeader("Content-Type", "application/json");
			req.SendWebRequest();
		}
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace BuildSystem
{
	public static class BuildScript
	{
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
			
			var options = settings.GetBuildOptions();
			var report = BuildPipeline.BuildPlayer(options);

			if (report.summary.result != BuildResult.Succeeded)
			{
				EditorApplication.Exit(1);
				return;
			}

			Console.WriteLine($"Output Path: {report.summary.outputPath}");
			CleanUp(settings.LocationPath);
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
	}
}
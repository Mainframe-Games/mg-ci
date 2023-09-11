using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildSystem.Utils
{
	[InitializeOnLoad]
	public static class AppVersionCreator
	{
		static AppVersionCreator()
		{
			CreateAppVersionFile();
		}

		private static void CreateAppVersionFile()
		{
			var path = Path.Combine(Application.streamingAssetsPath, AppVersion.FILE_NAME);
			var fileInfo = new FileInfo(path);

			if (fileInfo.Exists)
				return;

			fileInfo.Directory?.Create();

			var fullVersion = $"{Application.version}.{PlayerSettings.macOS.buildNumber}";
			File.WriteAllText(path, fullVersion);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}
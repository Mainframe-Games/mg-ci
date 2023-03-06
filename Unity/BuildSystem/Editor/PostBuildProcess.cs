using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildSystem
{
	public class PostBuildProcess : IPostprocessBuildWithReport
	{
		public int callbackOrder { get; }

		public void OnPostprocessBuild(BuildReport report)
		{
			var outputFile = new FileInfo(report.summary.outputPath);
			var rootDir = outputFile.Directory;
			foreach (var directory in rootDir.GetDirectories())
			{
				if (DontShip(directory.Name))
					DeleteDir(directory.FullName);
			}
		}

		private static bool DontShip(string dirName)
		{
			return dirName.Contains("DoNotShip")
				|| dirName.Contains("DontShip");
		}

		private static void DeleteDir(string path)
		{
			if (!Directory.Exists(path))
				return;
			
			Debug.Log($"[{nameof(PostBuildProcess)}] Deleting directory: {path}");
			Directory.Delete(path, true);
		}
	}
}
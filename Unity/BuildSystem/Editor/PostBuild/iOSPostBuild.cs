using BuildSystem.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BuildSystem.PostBuild
{
	public class iOSPostBuild : IPostprocessBuildWithReport
	{
		public int callbackOrder { get; }

		public void OnPostprocessBuild(BuildReport report)
		{
			if (report.summary.platform is not BuildTarget.iOS and not BuildTarget.tvOS)
				return;
			
			var settings = BuildScript.CurrentBuildSettings;
			if (!settings)
				return;

			var outputPath = report.summary.outputPath;
			BS_Logger.Log($"[{nameof(iOSPostBuild)}] {report.summary.platform} PostProcess. Path: {outputPath}");

			// var pbx = new PBXHelper(outputPath);
			UpdateInfoPlist(settings, outputPath);
		}
		
		private static void UpdateInfoPlist(BuildSettings settings, string outputPath)
		{
			var plist = new PListHelper(outputPath);
			SetPListElements(settings, plist);
			plist.Save();
		}

		private static void SetPListElements(BuildSettings settings, PListHelper plist)
		{
			if (!settings)
				return;
			
			foreach (var p in settings.PListElementBools)
			{
				plist.SetBoolean(p.Key, p.Value);
				BS_Logger.Log($"[iOSPostProcessor] Added Info.plist {p.Key}: {p.Value}");
			}

			foreach (var p in settings.PListElementFloats)
			{
				plist.SetFloat(p.Key, p.Value);
				BS_Logger.Log($"[iOSPostProcessor] Added Info.plist {p.Key}: {p.Value}");
			}

			foreach (var p in settings.PListElementInts)
			{
				plist.SetInteger(p.Key, p.Value);
				BS_Logger.Log($"[iOSPostProcessor] Added Info.plist {p.Key}: {p.Value}");
			}

			foreach (var p in settings.PListElementStrings)
			{
				plist.SetString(p.Key, p.Value);
				BS_Logger.Log($"[iOSPostProcessor] Added Info.plist {p.Key}: {p.Value}");
			}
		}
	}
}
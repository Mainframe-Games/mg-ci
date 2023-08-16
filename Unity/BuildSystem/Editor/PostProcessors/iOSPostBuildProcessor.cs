using BuildSystem.Utils;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace BuildSystem.PostProcessors
{
	public static class iOSPostBuildProcessor
	{
		public static void Process(BuildSettings settings, BuildReport report)
		{
			if (report.summary.platform is not BuildTarget.iOS and not BuildTarget.tvOS)
				return;
			
			var outputPath = report.summary.outputPath;
			BS_Logger.Log($"{report.summary.platform} PostProcess. Path: {outputPath}");

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
using BuildSystem.Utils;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BuildSystem.PostBuild
{
	public class PostBuildTest : IPostprocessBuildWithReport
	{
		public int callbackOrder { get; }

		public void OnPostprocessBuild(BuildReport report)
		{
			var config = BuildScript.CurrentBuildSettings;
			BS_Logger.Log($"[{nameof(PostBuildTest)}] Config: {config}");
		}
	}
}
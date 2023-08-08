#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace BuildSystem.Utils
{
	public class PBXHelper
	{
		public string buildPath { get; private set; }
		public string projectPath { get; private set; }
#if UNITY_IOS
		public PBXProject project { get; private set; }
#endif
		public string mainTarget { get; private set; } // app wrapper
		public string frameworkTarget { get; private set; } // game

		// main configs
		public string debugConfig { get; private set; }
		public string releaseConfig { get; private set; }

		public PBXHelper(string buildPath)
		{
			this.buildPath = buildPath;
#if UNITY_IOS
			projectPath = PBXProject.GetPBXProjectPath(buildPath);

			project = new PBXProject();
			project.ReadFromFile(projectPath);

			mainTarget = project.GetUnityMainTargetGuid();
			frameworkTarget = project.GetUnityFrameworkTargetGuid();

			// main configs
			debugConfig = project.BuildConfigByName(mainTarget, "Debug");
			releaseConfig = project.BuildConfigByName(mainTarget, "Release");
#endif
		}

		public void Save()
		{
#if UNITY_IOS
			project.WriteToFile(projectPath);
#endif
		}
	}
}

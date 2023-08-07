using UnityEditor.iOS.Xcode;

namespace BuildSystem.Utils
{
	public class PBXHelper
	{
		public string buildPath { get; private set; }
		public string projectPath { get; private set; }
		public PBXProject project { get; private set; }

		public string mainTarget { get; private set; } // app wrapper
		public string frameworkTarget { get; private set; } // game

		// main configs
		public string debugConfig { get; private set; }
		public string releaseConfig { get; private set; }


		public PBXHelper(string buildPath)
		{
			this.buildPath = buildPath;
			projectPath = PBXProject.GetPBXProjectPath(buildPath);

			project = new PBXProject();
			project.ReadFromFile(projectPath);

			mainTarget = project.GetUnityMainTargetGuid();
			frameworkTarget = project.GetUnityFrameworkTargetGuid();

			// main configs
			debugConfig = project.BuildConfigByName(mainTarget, "Debug");
			releaseConfig = project.BuildConfigByName(mainTarget, "Release");
		}

		public void Save()
		{
			project.WriteToFile(projectPath);
		}
	}
}


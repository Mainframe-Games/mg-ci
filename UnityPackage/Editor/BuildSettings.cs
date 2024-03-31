using System.Linq;
using BuildSystem.PostProcessors.PList;
using UnityEditor;
using UnityEngine;

namespace BuildSystem
{
	public class BuildSettings
	{
		[Header("Build Config")]
		[Tooltip("File extension. Include '.'")]
		public string Extension;
		public string ProductName;
		public BuildTarget Target = BuildTarget.StandaloneWindows64;
		public BuildTargetGroup TargetGroup = BuildTargetGroup.Standalone;
		public StandaloneBuildSubtarget SubTarget = StandaloneBuildSubtarget.Player;
		[Tooltip("Location to build player. Can use -buildPath CLI param to override")]
		public string BuildPath = "Builds/";
		[Tooltip("Custom scenes overrides. Empty array will use EditorSettings.Scenes")]
		public SceneAsset[] Scenes;
		[Tooltip("Custom define overrides. Empty array will use ProjectSettings defines")]
		public string[] ExtraScriptingDefines;
		public string AssetBundleManifestPath;
		public BuildOptions BuildOptions = BuildOptions.None;

		[Header("Optional")]
		[Tooltip("Deletes all the files at LocationPath before building")]
		public bool DeleteFiles;

		[Tooltip("If true will strip IL2CPP folders like _BackUpThisFolder_ButDontShipItWithYourGame")]
		public bool StripDontShipFolders;

		[Header("Android")] 
		public string KeystorePath;
		public string KeystoreAlias;
		public string KeystorePassword;

		[Header("iOS")]
		public PListElementBool[] PListElementBools;
		public PListElementString[] PListElementStrings;
		public PListElementInt[] PListElementInts;
		public PListElementFloat[] PListElementFloats;

		public static string[] GetEditorSettingsScenes()
		{
			return EditorBuildSettings.scenes
				.Where(x => x.enabled)
				.Select(x => x.path)
				.ToArray();
		}
	}
}
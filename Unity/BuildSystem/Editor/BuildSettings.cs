using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildSystem
{
	[CreateAssetMenu(fileName = "BuildSettings", menuName = "Builds/New Settings")]
	public class BuildSettings : ScriptableObject
	{
		public string Extension = ".exe";
		public BuildTarget Target = BuildTarget.StandaloneWindows64;
		public StandaloneBuildSubtarget SubTarget = StandaloneBuildSubtarget.Player;
		public BuildTargetGroup TargetGroup = BuildTargetGroup.Standalone;
		public string LocationPath = "Builds/";
		public string[] Scenes;
		public string[] ScriptingDefines;
		public string AssetBundleManifestPath;
		public BuildOptions BuildOptions = BuildOptions.None;

		public BuildPlayerOptions GetBuildOptions()
		{
			var options = new BuildPlayerOptions
			{
				target = Target,
				subtarget = (int)SubTarget,
				locationPathName = Path.Combine(LocationPath, $"{Application.productName}{Extension}"),
				targetGroup = TargetGroup,
				assetBundleManifestPath = AssetBundleManifestPath,
				scenes = Scenes,
				extraScriptingDefines = ScriptingDefines,
				options = BuildOptions,
			};
			return options;
		}

		private void OnEnable()
		{
			IsValid();
		}

		[ContextMenu("Set Scenes")]
		private void PopulateScenes()
		{
			Scenes = EditorBuildSettings.scenes
				.Where(x => x.enabled)
				.Select(x => x.path)
				.ToArray();
		}
		
		[ContextMenu("Set Defines")]
		private void PopulateDefines()
		{
			ScriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(TargetGroup)
				.Split(";")
				.Where(x => !string.IsNullOrEmpty(x))
				.Distinct()
				.ToArray();
		}

		[ContextMenu("Validate")]
		public bool IsValid()
		{
			// validate scenes
			var editorScenes = EditorBuildSettings.scenes.Select(x => x.path).ToList();

			foreach (var scene in Scenes)
			{
				if (editorScenes.Contains(scene))
					continue;
				
				Debug.LogError($"Scene isn't in build settings '{scene}'");
				return false;
			}

			return true;
		}

		[ContextMenu("Build")]
		private void Build()
		{
			BuildScript.BuildPlayer(this);
		}
	}
}
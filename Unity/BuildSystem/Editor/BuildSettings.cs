using System.IO;
using System.Linq;
using BuildSystem.PostProcessors.PList;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace BuildSystem
{
	[CreateAssetMenu(fileName = "BuildSettings", menuName = "Build System/New Settings")]
	public class BuildSettings : ScriptableObject
	{
		[Header("File")]
		public string Name;
		
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
		[FormerlySerializedAs("ScriptingDefines")] 
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

		public BuildPlayerOptions GetBuildOptions(string rootDirectoryPath = null)
		{
			// if no override given use default
			if (string.IsNullOrEmpty(rootDirectoryPath))
				rootDirectoryPath = BuildPath;
			
			var scenes = Scenes.Length > 0 
				? Scenes.Select(AssetDatabase.GetAssetPath).ToArray()
				: GetEditorSettingsScenes();
			
			var options = new BuildPlayerOptions
			{
				target = Target,
				subtarget = (int)SubTarget,
				locationPathName = Path.Combine(rootDirectoryPath, $"{ProductName}{Extension}"),
				targetGroup = TargetGroup,
				assetBundleManifestPath = AssetBundleManifestPath,
				scenes = scenes,
				extraScriptingDefines = ExtraScriptingDefines,
				options = BuildOptions
			};

			if (ExtraScriptingDefines.Length > 0)
				options.extraScriptingDefines = ExtraScriptingDefines;
			
			return options;
		}

		private void OnValidate()
		{
			AutoSetExtension();
		}

		private void AutoSetExtension()
		{
			if (string.IsNullOrEmpty(Name))
				Name = name.Replace(nameof(BuildSettings), string.Empty).Trim('_');
			
			if (string.IsNullOrEmpty(ProductName))
				ProductName = Application.productName;

			_SetDirty();
		}

		private static string[] GetEditorSettingsScenes()
		{
			return EditorBuildSettings.scenes
				.Where(x => x.enabled)
				.Select(x => x.path)
				.ToArray();
		}
		
		[ContextMenu("Set Defines")]
		private void PopulateDefines()
		{
			ExtraScriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(TargetGroup)
				.Split(";")
				.Where(x => !string.IsNullOrEmpty(x))
				.Distinct()
				.ToArray();
		}

		[ContextMenu("Validate")]
		public bool IsValid()
		{
			return true;
		}

		[ContextMenu("Build")]
		private void Build()
		{
			BuildScript.BuildPlayer(this);
		}
		
		[ContextMenu("Set Dirty")]
		private void _SetDirty()
		{
			EditorUtility.SetDirty(this);
		}
	}
}
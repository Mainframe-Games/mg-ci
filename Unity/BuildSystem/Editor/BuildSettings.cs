using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace BuildSystem
{
	[CreateAssetMenu(fileName = "BuildSettings", menuName = "Build Settings/New Settings")]
	public class BuildSettings : ScriptableObject
	{
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
		[Tooltip("Enter SteamId for when you need different steam deployments. i.e Demo and official builds")]
		public ulong SteamId;

		[Tooltip("Deletes all the files at LocationPath before building")]
		public bool DeleteFiles;

		[Tooltip("Runs 'AddressableAssetSettings.BuildPlayerContent' before player build")]
		public bool BuildAddressables;

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
			// TODO: support more targets just cant be bothered right now. Its all I need
			Extension = Target switch
			{
				BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => ".exe",
				BuildTarget.StandaloneOSX => ".app",
				BuildTarget.StandaloneLinux64 => ".x86_64",
				_ => Extension
			};

			if (string.IsNullOrEmpty(ProductName))
				ProductName = Application.productName;
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
			return EnsureSteamId();
		}

		[ContextMenu("Build")]
		private void Build()
		{
			BuildScript.BuildPlayer(this);
		}
		
		private bool EnsureSteamId()
		{
			if (SteamId == 0)
				return true;

			const string steamAppId = "steam_appid.txt";
			
			if (!File.Exists(steamAppId))
				return true;

			var curSteamId = ulong.Parse(File.ReadAllText(steamAppId));
			if (SteamId != curSteamId)
				File.WriteAllText(steamAppId, SteamId.ToString());
			return true;
		}
	}
}
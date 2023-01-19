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

		[Header("Optional")]
		[Tooltip("Enter SteamId for when you need different steam deployments. i.e Demo and official builds")]
		public ulong SteamId;
		
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
			return EnsureSteamId() && EnsureScenes();
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

		private bool EnsureScenes()
		{
			if (Scenes == null)
				return false;
			
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
	}
}
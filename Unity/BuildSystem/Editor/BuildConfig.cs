using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BuildSystem.Utils;
using UnityEditor;
using UnityEngine;

namespace BuildSystem
{
	[CreateAssetMenu(fileName = "BuildConfig", menuName = "Build System/New Config", order = 0)]
	public class BuildConfig : ScriptableObject
	{
		[Header("Meta Data")]
		[Tooltip("Used as a link to the store for the title of the Discord embedded message")]
		public string Url;
		[Tooltip("Used for thumbnail image for Discord embedded message")]
		public string ThumbnailUrl;
		
		[Header("Settings")]
        public PreBuild PreBuild;
		public Build Build;
		public Deploy Deploy;
		public WebHook[] Hooks;

		[ContextMenu("Set Dirty")]
		private void OnValidate()
		{
			EditorUtility.SetDirty(this);
		}

		[ContextMenu("Build Json")]
		private void BuildJson()
		{
			var json = JsonUtility.ToJson(this, true);
			Debug.Log($"{Environment.CurrentDirectory} {json}");
		}
		
		public static BuildConfig GetOrCreateSettings()
		{
			var settings = AssetFinder.GetAsset<BuildConfig>();

			if (settings)
				return settings;
			
			settings = CreateInstance<BuildConfig>();
			AssetDatabase.CreateAsset(settings, "Assets/Settings/BuildSettings/BuildConfig.asset");
			AssetDatabase.SaveAssets();
			return settings;
		}

		/// <summary>
		/// Returns all public fields as searchable keywords
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<string> GetKeywords()
		{
			return typeof(BuildConfig)
				.GetFields(BindingFlags.Public | BindingFlags.Instance)
				.Select(x => x.Name)
				.Distinct();
		}
	}

	[Serializable]
	public struct PreBuild
	{
		public int BumpIndex;
		public Versions Versions;
	}
	
	[Serializable]
	public struct Versions
	{
		public bool BundleVersion;
		public bool AndroidVersionCode;
		public string[] BuildNumbers;
	}
	
	[Serializable]
	public struct Build
	{
		public BuildSettings[] BuildTargets;
	}

	[Serializable]
	public struct Deploy
	{
		public string[] Steam;
		public bool	AppleStore;
		public bool	GoogleStore;	
		public bool	Clanforge;
		public bool	S3;
	}
	
	[Serializable]
	public struct WebHook
	{
		public string Title;
		public string Url;
		public bool IsErrorChannel;
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BuildSystem.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace BuildSystem
{
	[CreateAssetMenu(fileName = "BuildConfig", menuName = "Build System/New Config", order = 0)]
	public class BuildConfig : ScriptableObject
	{
		[JsonIgnore]
		private string PATH => $"BuildSystem/{name}.json";
		
		[Header("Meta Data")]
		public Meta Meta;
		
		[Header("Settings")]
        public PreBuild PreBuild;
		public Build Build;
		public Deploy Deploy;
		public WebHook[] Hooks;

		public override string ToString()
		{
			return Json.Serialise(this);
		}

		private void OnEnable()
		{
			Meta = Meta.Load();
		}

		[ContextMenu("Set Dirty")]
		private void OnValidate()
		{
			EditorUtility.SetDirty(this);
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

		public void Save()
		{
			Meta.Save();
			// SaveLoad.Save(PATH, this);
		}
	}
	
	[Serializable]
	public struct Meta
	{
		private const string PATH = "BuildSystem/WorkspaceMeta.json";

		public string ProjectName;
		[Tooltip("Used as a link to the store for the title of the Discord embedded message")]
		public string Url;
		[Tooltip("Used for thumbnail image for Discord embedded message")]
		public string ThumbnailUrl;
		[Tooltip("Last successful build changeset id. Automatically saved by build server but you can manually set here too")]
		public int LastSuccessfulBuild;
		
		public static Meta Load()
		{
			if (SaveLoad.TryLoad(PATH, out Meta meta))
				return meta;
			
			// return default
			return new Meta
			{
				ProjectName = Application.productName
			};
		}
		
		public void Save()
		{
			SaveLoad.Save(PATH, this);
		}
	}

	[Serializable]
	public struct PreBuild
	{
		public bool BuildNumberStandalone;
		public bool BuildNumberIphone;
		public bool AndroidVersionCode;
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
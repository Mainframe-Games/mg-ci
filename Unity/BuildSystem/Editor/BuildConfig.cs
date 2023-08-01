using System;
using UnityEditor;
using UnityEngine;

namespace BuildSystem
{
	[CreateAssetMenu(fileName = "BuildConfig", menuName = "Build System/New Config", order = 0)]
	public class BuildConfig : ScriptableObject
	{
		public PreBuild PreBuild;
		public Build Build;
		public Deploy Deploy;
		public WebHook[] Hooks;

		[ContextMenu("Set Dirty")]
		private void OnValidate()
		{
			EditorUtility.SetDirty(this);
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
using System.Linq;
using Mainframe.CI.Editor.PostProcessors.PList;
using UnityEditor;
using UnityEngine;

namespace Mainframe.CI.Editor
{
	public class BuildSettings
	{
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
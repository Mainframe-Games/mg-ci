using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildSystem.Utils
{
	public static class ScriptableObjectCreator
	{
		public static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
		{
			var asset = AssetFinder.GetAsset<T>();

			if (asset)
				return asset;

			// create dir
			var fileInfo = new FileInfo(path);
			fileInfo.Directory?.Create();
			
			asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
		}
	}
}
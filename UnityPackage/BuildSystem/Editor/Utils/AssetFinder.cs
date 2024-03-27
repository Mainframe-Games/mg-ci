using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BuildSystem.Utils
{
	public static class AssetFinder
	{
		public static T GetAsset<T>(Func<T, bool> predicate = null) where T : Object
		{
			var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

			for (int i = 0; i < guids.Length; i++)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);
				var asset = AssetDatabase.LoadAssetAtPath<T>(path);

				// if no predicate and asset is not null return first found
				if (predicate == null && asset)
					return asset;

				// otherwise check against predicate
				if (predicate?.Invoke(asset) is true)
					return asset;
			}

			return null;
		}
	}
}
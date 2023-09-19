using System.IO;
using UnityEngine;

namespace BuildSystem.Utils
{
	public static class SaveLoad
	{
		public static void Save(string path, object obj)
		{
			var json = Json.Serialise(obj);
			var fileInfo = new FileInfo(path);
			fileInfo.Directory?.Create();
			Debug.Log($"Saving... {fileInfo.FullName} {json}");
			File.WriteAllText(fileInfo.FullName, json);
		}
		
		public static bool TryLoad<T>(string path, out T obj)
		{
			if (!File.Exists(path))
			{
				obj = default;
				return false;
			}
			
			var txt = File.ReadAllText(path);
			obj = Json.Deserialise<T>(txt);
			return obj is not null;
		}
	}
}
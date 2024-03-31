using System.IO;
using UnityEngine;

namespace BuildSystem
{
	/// <summary>
	/// Used to diff versions built locally and streamline deployed to clanforge
	/// </summary>
	public static class InternalVersion
	{
		public static string Version
		{
			get => File.Exists(Path) ? File.ReadAllText(Path) : Application.version;
			private set => File.WriteAllText(Path, value);
		}

		private static string Path => $"{Application.streamingAssetsPath}/internal-version.txt";

		private static int _ver
		{
			get => PlayerPrefs.GetInt(nameof(InternalVersion), 0);
			set => PlayerPrefs.SetInt(nameof(InternalVersion), value);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void Load()
		{
			if (Version != Application.version)
				Debug.Log($"Internal Version: {Version}");
		}

		public static void Bump()
		{
			Version = $"{Application.version}.{++_ver}";
			Debug.Log($"Internal Version Bump: {Version}");
		}
	}
}
using System.IO;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace BuildSystem.Utils
{
	public class PListHelper
	{
		public string basePath { get; private set; }
		public string plistFile { get; private set; }
		public string plistPath { get; private set; }

#if UNITY_IOS
		public PlistDocument doc { get; private set; }
		public PlistElementDict root { get; private set; }
#endif

		public PListHelper(string basePath) : this(basePath, "Info.plist")
		{
		}

		public PListHelper(string basePath, string plistFile)
		{
			this.basePath = basePath;
			this.plistFile = plistFile;

			var isPackage = basePath.EndsWith(".app") || basePath.EndsWith(".bundle");
			plistPath = Path.Combine(basePath, isPackage ? $"Contents/{plistFile}" : plistFile);

#if UNITY_IOS
			doc = new PlistDocument();
			doc.ReadFromFile(plistPath);
			root = doc.root;
#endif
		}

		public void Save()
		{
#if UNITY_IOS
			doc.WriteToFile(plistPath);
#endif
		}

		public void SetBoolean(string pKey, bool pValue)
		{
#if UNITY_IOS
			root.SetBoolean(pKey, pValue);
#endif
		}

		public void SetFloat(string pKey, float pValue)
		{
#if UNITY_IOS
			root.SetReal(pKey, pValue);
#endif
		}

		public void SetInteger(string pKey, int pValue)
		{
#if UNITY_IOS
            root.SetInteger(pKey, pValue);
#endif
		}

		public void SetString(string pKey, string pValue)
		{
#if UNITY_IOS
            root.SetString(pKey, pValue);
#endif	
		}
	}
}

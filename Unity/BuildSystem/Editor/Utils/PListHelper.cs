using UnityEditor.iOS.Xcode;
using System.IO;

namespace BuildSystem.Utils
{
	public class PListHelper
	{
		public string basePath { get; private set; }
		public string plistFile { get; private set; }
		public string plistPath { get; private set; }

		public PlistDocument doc { get; private set; }
		public PlistElementDict root { get; private set; }


		public PListHelper(string basePath) : this(basePath, "Info.plist")
		{
		}

		public PListHelper(string basePath, string plistFile)
		{
			this.basePath = basePath;
			this.plistFile = plistFile;

			var isPackage = basePath.EndsWith(".app") || basePath.EndsWith(".bundle");
			plistPath = Path.Combine(basePath, isPackage ? $"Contents/{plistFile}" : plistFile);

			doc = new PlistDocument();
			doc.ReadFromFile(plistPath);
			root = doc.root;
		}

		public void Save()
		{
			doc.WriteToFile(plistPath);
		}
	}
}

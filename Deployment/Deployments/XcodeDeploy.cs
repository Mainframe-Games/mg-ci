using SharedLib;

namespace Deployment.Deployments;

public static class XcodeDeploy
{
	private const string PROJECT = "Unity-iPhone.xcodeproj";
	private const string SCHEME = "Unity-iPhone";
	private const string CONFIG = "Release";

	public static void Deploy(string workingDir, string appleId, string appSpecificPassword, string exportOptionsPlist)
	{
		var originalDir = Environment.CurrentDirectory;
		Environment.CurrentDirectory = workingDir;

		const string projectName = "AppArchive";
		const string exportPath = "IpaBuild";

		// clean
		Xcodebuild(
			$"-project {PROJECT} -scheme {SCHEME} -sdk iphoneos -configuration {CONFIG} clean");
		
		// archive
		Xcodebuild(
			$"-project {PROJECT} -scheme {SCHEME} -sdk iphoneos archive -configuration {CONFIG} " +
			$"-archivePath \"XCodeArchives/{projectName}.xcarchive\"");
		
		// copy exportOptions to folder
		File.Copy(exportOptionsPlist, "exportOptions.plist", true);
		
		// export ipa file
		Xcodebuild($"-exportArchive -archivePath \"XCodeArchives/{projectName}.xcarchive\" " +
		           $"-exportOptionsPlist exportOptions.plist -exportPath \"{exportPath}\" -allowProvisioningUpdates");

		// upload
		var ipaName = new DirectoryInfo(exportPath).GetFiles("*.ipa").First().Name;
		XcRun($"altool --upload-app -f \"{exportPath}/{ipaName}\" -t ios -u {appleId} -p {appSpecificPassword}");

		Environment.CurrentDirectory = originalDir;
	}

	private static void Xcodebuild(string args)
	{
		var (code, output ) = Cmd.Run("xcodebuild", args);
		
		if (code != 0)
			throw new Exception(output);
	}

	private static void XcRun(string args)
	{
		var (code, output ) = Cmd.Run("xcrun", args);

		if (code != 0)
			throw new Exception(output);
	}
}
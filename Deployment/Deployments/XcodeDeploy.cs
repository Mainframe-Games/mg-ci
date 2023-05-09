using SharedLib;

namespace Deployment.Deployments;

public static class XcodeDeploy
{
	private const string PROJECT = "Unity-iPhone.xcodeproj";
	private const string SCHEME = "Unity-iPhone";
	private const string CONFIG = "Release";

	public static void Deploy(string appleId, string appSpecificPassword)
	{
		var originalDir = Environment.CurrentDirectory;
		Environment.CurrentDirectory = "Builds/ios/";

		const string projectName = "AppArchive";
		const string exportPath = "IpaBuild";

		// clean
		Cmd.Run("xcodebuild",
			$"-project {PROJECT} -scheme {SCHEME} -sdk iphoneos -configuration {CONFIG} clean");

		// archive
		Cmd.Run("xcodebuild",
			$"-project {PROJECT} -scheme {SCHEME} -sdk iphoneos archive -configuration {CONFIG} " +
			$"-archivePath \"XCodeArchives/{projectName}.xcarchive\"");

		// copy exportOptions to folder
		File.Copy("../../BuildScripts/ios/exportOptions.plist", "exportOptions.plist", true);

		// export ipa file
		Cmd.Run("xcodebuild",
			$"-exportArchive -archivePath \"XCodeArchives/{projectName}.xcarchive\" " +
			$"-exportOptionsPlist exportOptions.plist -exportPath \"{exportPath}\" -allowProvisioningUpdates");

		// upload
		var ipaName = new DirectoryInfo($"{Environment.CurrentDirectory}/{exportPath}").GetFiles("*.ipa").First().Name;
		Cmd.Run("xcrun",
			$"altool --upload-app -f \"{exportPath}/{ipaName}\" -t ios -u {appleId} -p {appSpecificPassword}");

		Environment.CurrentDirectory = originalDir;
	}
}
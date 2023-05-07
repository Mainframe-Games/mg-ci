using SharedLib;

namespace Deployment.Deployments;

public class XcodeDeploy
{
	private const string PROJECT = "Unity-iPhone.xcodeproj";
	private const string SCHEME = "Unity-iPhone";
	private const string CONFIG = "Release";
	

	public void Deploy(string projectName, string exportPath, string appleId, string applePassword)
	{
		(int exitCode, string output) = (0, "");
		
		// clean
		(exitCode, output) = Cmd.Run("xcodebuild",
			$"-project {PROJECT} -scheme {SCHEME} -sdk iphoneos -configuration {CONFIG} clean");
		
		ThrowIfError(exitCode, output);
		
		// archive
		(exitCode, output) = Cmd.Run("xcodebuild",
			$"-project {PROJECT} -scheme {SCHEME} -sdk iphoneos archive -configuration {CONFIG} -archivePath \"../XCodeArchives/{projectName}.xcarchive\"");
		
		ThrowIfError(exitCode, output);
		
		// export ipa file
		(exitCode, output) = Cmd.Run("xcodebuild",
			$"-exportArchive -archivePath ../XCodeArchives/{projectName}.xcarchive " +
			$"-exportOptionsPlist exportOptions.plist -exportPath \"{exportPath}\" -allowProvisioningUpdates");
		
		ThrowIfError(exitCode, output);
		return;
		// upload 
		/*
		 * cd /Applications/Xcode.app/Contents/Applications/Application\ Loader.app/Contents/Frameworks/ITunesSoftwareService.framework/Versions/A/Support/
		 * ./altool — upload-app -f {abs path to your project}/build/{your release scheme name}.ipa -u {apple id to publish the app} -p {password of apple id}
		 */
		const string path = @"/Applications/Xcode.app/Contents/Applications/Application Loader.app/Contents/Frameworks/ITunesSoftwareService.framework/Versions/A/Support/";
		var dir = new DirectoryInfo(path);
		if (!dir.Exists)
			throw new DirectoryNotFoundException(path);
		
		(exitCode, output) = Cmd.Run("altool", $"— upload-app -f {exportPath}.ipa -u {appleId} -p {applePassword}");
		
		ThrowIfError(exitCode, output);
	}

	private static void ThrowIfError(int exitCode, string output)
	{
		if (exitCode != 0)
			throw new Exception(output);
	}
}
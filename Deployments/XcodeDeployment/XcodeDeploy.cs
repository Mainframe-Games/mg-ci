using System.Diagnostics;

namespace XcodeDeployment;

public class XcodeDeploy(string projectId, string appleId, string appSpecificPassword)
{
    private const string PROJECT = "Unity-iPhone.xcodeproj";
    private const string SCHEME = "Unity-iPhone";
    private const string CONFIG = "Release";

    public void Deploy()
    {
        if (!OperatingSystem.IsMacOS())
            throw new Exception("XcodeDeploy can only be run on macOS");

        var env = Environment.GetEnvironmentVariable("XCODE_DEPLOY");
        var proj = Environment.GetEnvironmentVariable("XCODE_PROJECT");
        var scheme = Environment.GetEnvironmentVariable("XCODE_SCHEME");
        var config = Environment.GetEnvironmentVariable("XCODE_CONFIG");

        // TODO: find project in ci-cache with projectId

        // TODO: find path to exportOptions.plist
        var exportOptionsPlist = "TODO:path/to/exportOptions.plist";

        if (!File.Exists(exportOptionsPlist))
            throw new FileNotFoundException(exportOptionsPlist);

        // TODO: find path to working directory
        var workingDir = "TODO:path/to/workingDir";

        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = workingDir;

        const string projectName = "AppArchive";
        const string exportPath = "IpaBuild";

        // clean
        Xcodebuild(
            $"-project {PROJECT} -scheme {SCHEME} -sdk iphoneos -configuration {CONFIG} clean"
        );

        // archive
        Xcodebuild(
            $"-project {PROJECT} -scheme {SCHEME} -sdk iphoneos archive -configuration {CONFIG} "
                + $"-archivePath \"XCodeArchives/{projectName}.xcarchive\""
        );

        // copy exportOptions to folder
        File.Copy(exportOptionsPlist, "exportOptions.plist", true);

        // export ipa file
        Xcodebuild(
            $"-exportArchive -archivePath \"XCodeArchives/{projectName}.xcarchive\" "
                + $"-exportOptionsPlist exportOptions.plist -exportPath \"{exportPath}\" -allowProvisioningUpdates"
        );

        // upload
        var ipaName = new DirectoryInfo(exportPath).GetFiles("*.ipa").First().Name;
        XcRun(
            $"altool --upload-app -f \"{exportPath}/{ipaName}\" -t ios -u {appleId} -p {appSpecificPassword}"
        );

        Environment.CurrentDirectory = originalDir;

        Console.WriteLine("Xcode Deploy COMPLETED");
    }

    private void Xcodebuild(string args)
    {
        var (code, output) = Cmd.Run("xcodebuild", args);

        if (code != 0)
            throw new Exception(output);
    }

    private void XcRun(string args)
    {
        var (code, output) = Cmd.Run("xcrun", args);

        if (code != 0)
            throw new Exception(output);
    }

    private static class Cmd
    {
        public static (int, string) Run(string command, string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            return (process.ExitCode, output + error);
        }
    }
}

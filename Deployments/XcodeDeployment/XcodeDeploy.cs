using System.Diagnostics;

namespace XcodeDeployment;

public class XcodeDeploy
{
    private const string PROJECT = "Unity-iPhone.xcodeproj";
    private const string SCHEME = "Unity-iPhone";
    private const string CONFIG = "Release";

    private const string PROJECT_NAME = "AppArchive";
    private const string EXPORT_PATH = "IpaBuild";

    private readonly string _appleId;
    private readonly string _appSpecificPassword;

    private readonly string _workingDir;

    public XcodeDeploy(Guid projectGuid, string appleId, string appSpecificPassword)
    {
        _appleId = appleId;
        _appSpecificPassword = appSpecificPassword;
        var (workingDir, _) = ProjectFinder.GetProjectDirectory(projectGuid);
        _workingDir = workingDir.FullName;
    }

    public void Deploy()
    {
        if (!OperatingSystem.IsMacOS())
            throw new Exception("XcodeDeploy can only be run on macOS");

        // clean
        Xcodebuild(
            $"-project {PROJECT} -scheme {SCHEME} -sdk iphoneos -configuration {CONFIG} clean"
        );

        // archive
        Xcodebuild(
            $"-project {PROJECT} -scheme {SCHEME} -sdk iphoneos archive -configuration {CONFIG} "
                + $"-archivePath \"XCodeArchives/{PROJECT_NAME}.xcarchive\""
        );

        // copy exportOptions to folder
        var exportOptionsPlist = Path.Combine(_workingDir, ".ci", "exportOptions.plist");
        if (File.Exists(exportOptionsPlist))
            File.Copy(exportOptionsPlist, "exportOptions.plist", true);

        // export ipa file
        Xcodebuild(
            $"-exportArchive -archivePath \"XCodeArchives/{PROJECT_NAME}.xcarchive\" "
                + $"-exportOptionsPlist exportOptions.plist -exportPath \"{EXPORT_PATH}\" -allowProvisioningUpdates"
        );

        // upload
        var ipaName = new DirectoryInfo(EXPORT_PATH).GetFiles("*.ipa").First().Name;
        XcRun(
            $"altool --upload-app -f \"{EXPORT_PATH}/{ipaName}\" -t ios -u {_appleId} -p {_appSpecificPassword}"
        );

        Console.WriteLine("Xcode Deploy COMPLETED");
    }

    private void Xcodebuild(string args)
    {
        var (code, output) = Run("xcodebuild", args);
        if (code != 0)
            throw new Exception(output);
    }

    private void XcRun(string args)
    {
        var (code, output) = Run("xcrun", args);
        if (code != 0)
            throw new Exception(output);
    }

    private (int, string) Run(string command, string args)
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
                CreateNoWindow = true,
                WorkingDirectory = _workingDir
            }
        };

        process.Start();
        process.WaitForExit();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        return (process.ExitCode, output + error);
    }
}

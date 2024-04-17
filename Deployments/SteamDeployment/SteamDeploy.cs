using System.Diagnostics;
using System.Text;

namespace SteamDeployment;

public class SteamDeploy(AppBuild appBuild, string password, string username)
{
    private static string SteamCmdPath
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return Path.Combine("ContentBuilder", "builder", "steamcmd.exe");
            if (OperatingSystem.IsLinux())
                return Path.Combine("ContentBuilder", "builder_linux", "steamcmd.sh");
            if (OperatingSystem.IsMacOS())
                return Path.Combine("ContentBuilder", "builder_osx", "steamcmd.sh");

            throw new Exception("Operating system not supported");
        }
    }

    public void Deploy()
    {
        try
        {
            var vdf = appBuild.Build();
            var vdfPath = Path.Combine(appBuild.ContentRoot, "app_build.vdf");
            File.WriteAllText(vdfPath, vdf);

            var args = new StringBuilder();
            args.Append("+login");
            args.Append($" {username}");
            args.Append($" {password}");
            args.Append($" +run_app_build \"{vdfPath}\"");
            args.Append(" +quit");

            SetPermissions();
            var info = new ProcessStartInfo(SteamCmdPath)
            {
                Arguments = args.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false,
                
            };
            var process = Process.Start(info);
            process!.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.OutputDataReceived += (sender, eventArgs) =>
            {
                Console.WriteLine(eventArgs.Data);
            };
            process.ErrorDataReceived += (sender, eventArgs) =>
            {
                Console.WriteLine(eventArgs.Data);
                throw new Exception(eventArgs.Data);
            };
            process.WaitForExit();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void SetPermissions()
    {
        Chmod(SteamCmdPath);

        if (OperatingSystem.IsMacOS())
        {
            var steamCmd = SteamCmdPath.Replace(".sh", string.Empty);
            Chmod(steamCmd);
        }
    }

    private void Chmod(string pathToFile)
    {
        // no need to chmod on windows
        if (OperatingSystem.IsWindows())
            return;

        var info = new ProcessStartInfo
        {
            FileName = "chmod",
            Arguments = $"+x {pathToFile}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(info)!.WaitForExit();
    }
}
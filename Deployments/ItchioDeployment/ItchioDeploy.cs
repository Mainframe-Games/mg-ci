using System.Diagnostics;

namespace Deployment.Deployments;

public class ItchioDeploy(string location, string username, string gameName)
{
    private string ButlerExe =>
        Environment.OSVersion.Platform == PlatformID.Unix
            ? $"{location}/butler"
            : $"{location}/butler.exe";

    public void Deploy(IEnumerable<string> dirs, string version)
    {
        if (string.IsNullOrEmpty(version))
            throw new NullReferenceException("itchio version param is null");

        // butler push mygame user/mygame:win32-final --userversion 1.1.0
        foreach (var dir in dirs)
        {
            var target = dir.Split("/")[^1].Replace("_demo", "");
            Cmd.Run(
                ButlerExe,
                $"push \"{dir}\" {username}/{gameName}:{target} --userversion {version}"
            );
        }
    }

    private class Cmd
    {
        public static void Run(string exe, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception(process.StandardError.ReadToEnd());
        }
    }
}

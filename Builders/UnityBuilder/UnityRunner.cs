using System.Diagnostics;
using SharedLib;

namespace UnityBuilder;

internal class UnityRunner(string exePath)
{
    public int ExitCode { get; private set; }
    public string? Message { get; private set; }
    public TimeSpan Time { get; set; }

    public void Run(UnityArgs unityArgs)
    {
        var sw = Stopwatch.StartNew();
        var args = unityArgs.Build();
        var (exitCode, output) = Cmd.Run(exePath, args);
        ExitCode = exitCode;
        Message = output;
        Time = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);
    }
}

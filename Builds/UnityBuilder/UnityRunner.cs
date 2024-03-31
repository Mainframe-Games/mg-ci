using SharedLib;

namespace UnityBuilder;

internal class UnityRunner(string exePath)
{
    public int ExitCode { get; private set; }
    public string? Message { get; private set; }

    public void Run(UnityArgs unityArgs)
    {
        var args = unityArgs.Build();
        var (exitCode, output) = Cmd.Run(exePath, args);
        ExitCode = exitCode;
        Message = output;
    }
}

using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;

namespace AvaloniaAppMVVM.WebClient;

public static class ProcessManager
{
    private static readonly Queue<ProcessRunner> _processQueue = new();

    public static void StartBuild()
    {
        var runner = new ProcessRunner();
        runner.StartBuild();
        _processQueue.Enqueue(runner);
    }
}

public class ProcessRunner
{
    public ObservableCollection<IProcess> Template { get; } =
        [
            new CiProcess { Id = "PreBuild" },
            new CiProcess { Id = "Build" },
            new CiProcess { Id = "Deploy" },
            new CiProcess { Id = "Hooks" }
        ];

    public bool IsActive { get; private set; }

    public ProcessRunner() { }

    public void StartBuild()
    {
        foreach (var process in Template)
        {
            process.Run();
        }
    }
}

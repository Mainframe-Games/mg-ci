using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Media.Imaging;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaAppMVVM.ViewModels;

public partial class HomePageViewModel : ViewModelBase
{
    [ObservableProperty]
    private Project? _project;

    public ObservableCollection<IProcess> Processes { get; } =
    [
        new CiProcess
        {
            Id = "Pre Build",
            Succeeded = true,
            Logs = BuildFakeLog()
        },
        new CiProcess
        {
            Id = "Build",
            Failed = true,
            SubProcesses = 
            [
                new UnityBuildTarget
                {
                    Name = "Windows64",
                    Target = Unity.BuildTarget.StandaloneWindows64
                },
                new UnityBuildTarget
                {
                    Name = "MacOs",
                    Target = Unity.BuildTarget.StandaloneOSX
                }
            ]
        },
        new CiProcess
        {
            Id = "Deploy",
            IsBusy = true,
        },
        new CiProcess
        {
            Id = "Hooks",
            IsQueued = true,
            Logs = BuildFakeLog()
        }
    ];

    private static string BuildFakeLog()
    {
        var str = new StringBuilder();
        for (int i = 0; i < 100; i++)
            str.AppendLine($"Log line {i}...");
        return str.ToString();
    }

    public Task<Bitmap?> ImageSourceBitmapWeb =>
        ImageLoader.LoadFromWeb(
            "https://cdn.cloudflare.steamstatic.com/steam/apps/1622570/header.jpg?t=1693947196"
        );

    [RelayCommand]
    public void OpenStoreLinkCommand()
    {
        Console.WriteLine("Start build");
    }
}

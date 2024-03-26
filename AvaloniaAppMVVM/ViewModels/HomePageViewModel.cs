using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaAppMVVM.ViewModels;

public partial class HomePageViewModel : ViewModelBase
{
    [ObservableProperty]
    private Project? _project;

    public ObservableCollection<IProcess> Processes { get; } =
        [
            new CiProcess { Id = "PreBuild" },
            new CiProcess { Id = "Build" },
            new CiProcess { Id = "Deploy" },
            new CiProcess { Id = "Hooks" }
        ];
}

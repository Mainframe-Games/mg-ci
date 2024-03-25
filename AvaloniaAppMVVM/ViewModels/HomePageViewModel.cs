using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.WebClient;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaAppMVVM.ViewModels;

public partial class HomePageViewModel : ViewModelBase
{
    [ObservableProperty]
    private Project? _project;

    public ObservableCollection<IProcess> Processes { get; } = new ProcessRunner().Template;
}

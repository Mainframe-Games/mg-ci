using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaAppMVVM.ViewModels;

public partial class ProjectSettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private Project? _project;

    public ObservableCollection<VersionControlType> VersionControlOptions { get; } =
        new(Enum.GetValues<VersionControlType>());
}

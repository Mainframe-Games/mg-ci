using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaAppMVVM.ViewModels;

public partial class BuildTargetsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private string? _projectName;

    public ObservableCollection<UnityBuildTargetTemplate> BuildTargets { get; } = [];

    [ObservableProperty]
    private UnityBuildTargetTemplate? _selectedBuildTarget;
}

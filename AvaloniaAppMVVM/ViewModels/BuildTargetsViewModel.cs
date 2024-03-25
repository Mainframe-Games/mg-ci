using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaAppMVVM.ViewModels;

public partial class BuildTargetsViewModel : ViewModelBase
{
    public ObservableCollection<UnityBuildTarget> BuildTargets { get; } = [];

    [ObservableProperty]
    private UnityBuildTarget? _selectedBuildTarget;
}

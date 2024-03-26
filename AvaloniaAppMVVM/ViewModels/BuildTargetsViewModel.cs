using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.Data.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using ServerClientShared;

namespace AvaloniaAppMVVM.ViewModels;

public partial class BuildTargetsViewModel : ViewModelBase
{
    public ObservableCollection<UnityBuildTargetTemplate> BuildTargets { get; } = [];

    [ObservableProperty]
    private UnityBuildTarget? _selectedBuildTarget;
}

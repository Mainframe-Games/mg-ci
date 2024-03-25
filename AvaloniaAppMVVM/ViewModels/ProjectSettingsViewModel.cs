using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaAppMVVM.ViewModels;

public partial class ProjectSettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _projectName;

    [ObservableProperty]
    private string? _location;

    [ObservableProperty]
    private string? _versionControl;

    [ObservableProperty]
    private string? _gameEngine;

    [ObservableProperty]
    private string? _storeUrl;

    [ObservableProperty]
    private string? _storeThumbnailUrl;

    [ObservableProperty]
    private string? _lastSuccessfulBuild;

    public ObservableCollection<string> VersionControlOptions { get; } =
        new(Enum.GetNames(typeof(VersionControlType)));

    public ObservableCollection<string> GameEngineOptions { get; } =
        new(Enum.GetNames(typeof(GameEngineType)));
}

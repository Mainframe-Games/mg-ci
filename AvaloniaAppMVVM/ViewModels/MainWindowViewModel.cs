using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaAppMVVM.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tomlyn;

namespace AvaloniaAppMVVM.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AppSettings _appSettings;

    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private ViewModelBase _currentPage = new HomePageViewModel();

    [ObservableProperty]
    private ListItemTemplate? _selectedListItem;

    [ObservableProperty]
    private Project? _currentProject = new();

    /// <summary>
    /// Get icons from: https://avaloniaui.github.io/icons.html
    /// </summary>
    public ObservableCollection<ListItemTemplate> Items { get; } =
        [
            new ListItemTemplate(typeof(HomePageViewModel), "Home", "home_regular"),
            new ListItemTemplate(
                typeof(ProjectSettingsViewModel),
                "Project Settings",
                "edit_settings_regular"
            ),
            new ListItemTemplate(typeof(PrebuildViewModel), "Pre Build", "app_generic_regular"),
            new ListItemTemplate(typeof(BuildTargetsViewModel), "Build Targets", "target_regular"),
            new ListItemTemplate(typeof(DeployViewModel), "Deploy", "rocket_regular"),
            new ListItemTemplate(typeof(HooksViewModel), "Hooks", "share_android_regular"),
        ];

    public MainWindowViewModel()
    {
        // load settings
        var file = new FileInfo("settings.toml");
        _appSettings = file.Exists
            ? Toml.ToModel<AppSettings>(File.ReadAllText("settings.toml"))
            : new AppSettings();

        // load project
        LoadProject(_appSettings.LastProjectLocation);
    }

    public void OnAppClose()
    {
        _appSettings.LastProjectLocation = CurrentProject?.Location;
        SaveAppSettings();
    }

    private void SaveAppSettings()
    {
        _appSettings.LastProjectLocation = CurrentProject?.Location;

        var toml = Toml.FromModel(_appSettings);
        File.WriteAllText("settings.toml", toml);
        Console.WriteLine("Saved settings");
    }

    public void LoadProject(string? location)
    {
        if (string.IsNullOrEmpty(location))
            return;

        var toml = File.ReadAllText(Path.Combine(location, ".ci", "project.toml"));
        CurrentProject = Toml.ToModel<Project>(toml);
        Console.WriteLine($"Loading project: {CurrentProject.Location}");
    }

    [RelayCommand]
    public void TogglePaneCommand()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    partial void OnSelectedListItemChanged(ListItemTemplate? value)
    {
        CurrentPage = ViewLocator.GetViewModel(
            value?.ModelType ?? throw new NullReferenceException()
        );
    }

    [RelayCommand]
    public void Button_Github_OnClick()
    {
        const string url = "https://github.com/Mainframe-Games/mg-ci";
        Process.Start("explorer", url);
    }
}

public class ListItemTemplate
{
    public string Label { get; set; }
    public Type ModelType { get; set; }
    public StreamGeometry Icon { get; set; }

    public ListItemTemplate(Type modelType, string label, string iconKey)
    {
        ModelType = modelType;
        Label = label;

        Application.Current!.TryGetResource(iconKey, out var res);
        Icon = (StreamGeometry)res!;
    }
}

/// <summary>
/// A container for each workspace
/// </summary>
public class Project
{
    public string? Name { get; set; }
    public string? Location { get; set; }
    public ProjectSettings Settings { get; set; } = new();
}

public class ProjectSettings
{
    /// <summary>
    /// The version control system used for the project
    /// </summary>
    public VersionControlType VersionControl { get; set; }

    /// <summary>
    /// The game engine used for the project
    /// </summary>
    public GameEngineType GameEngine { get; set; }

    /// <summary>
    /// URL to game page
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Thumbnail image for page
    /// </summary>
    public string? Thumbnail { get; set; }

    /// <summary>
    /// Plastic: Changeset ID of last successful build
    /// <para></para>
    /// Git: Sha of last successful build
    /// </summary>
    public string? LastSuccessfulBuild { get; set; }
}

public enum VersionControlType
{
    Git,
    Plastic,
}

public enum GameEngineType
{
    Unity,
    Godot,
}

using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.WebClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tomlyn;

namespace AvaloniaAppMVVM.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private AppSettings _appSettings;

    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private ViewModelBase _currentPage = new HomePageViewModel();

    [ObservableProperty]
    private ListItemTemplate? _selectedListItem;

    [ObservableProperty]
    private Project? _currentProject = new();

    private readonly Client _client = new();

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

    public ObservableCollection<Project> ProjectOptions { get; } = [];

    public MainWindowViewModel()
    {
        // load settings
        var file = new FileInfo("settings.toml");
        _appSettings = file.Exists
            ? Toml.ToModel<AppSettings>(File.ReadAllText("settings.toml"))
            : new AppSettings();

        // load all projects
        foreach (var path in _appSettings.LoadedProjectPaths)
            ProjectOptions.Add(Project.Load(path));

        // load project
        LoadCurrentProject(_appSettings.LastProjectLocation);
    }

    public void OnAppClose()
    {
        _appSettings.LastProjectLocation = CurrentProject?.Location;
        SaveAppSettings();
    }

    private void SaveAppSettings()
    {
        var toml = Toml.FromModel(_appSettings);
        File.WriteAllText("settings.toml", toml);
        Console.WriteLine("Saved settings");
    }

    public void LoadCurrentProject(string? location)
    {
        if (string.IsNullOrEmpty(location))
            return;

        // return if already loaded
        if (CurrentProject?.Location == location)
            return;

        // load project
        CurrentProject = Project.Load(location);

        // add to combo box
        if (!AppSettings.LoadedProjectPaths.Contains(location))
            AppSettings.LoadedProjectPaths.Add(location);

        if (!ProjectOptions.Contains(CurrentProject))
            ProjectOptions.Add(CurrentProject);

        SaveAppSettings();
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

using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaApp;
using AvaloniaAppMVVM.ViewModels;
using FluentAvalonia.UI.Windowing;
using Tomlyn;

namespace AvaloniaAppMVVM.Views;

public partial class MainWindow : AppWindow
{
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();

        // TitleBar.ExtendsContentIntoTitleBar = true;
        // TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
        // SplashScreen = new ComplexSplashScreen();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        ViewModel.OnAppClose();
    }

    public void Button_Settings_OnClick(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Show(this);
    }

    private async void Button_OpenProject_OnClick(object? sender, RoutedEventArgs e)
    {
        //This can also be applied for SaveFilePicker.
        var folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Open Project", AllowMultiple = false }
        );

        if (folders.Count == 0)
        {
            Console.WriteLine("No folder selected");
            return;
        }

        var rootDir = new DirectoryInfo(folders[0].Path.AbsolutePath);
        var childDirs = rootDir.GetDirectories();
        if (childDirs.All(x => x.Name != ".ci"))
        {
            Console.WriteLine("No .ci folder found");
            return;
        }

        // good to go
        ViewModel.LoadProject(rootDir.FullName);
    }

    private async void Button_NewProject_OnClick(object? sender, RoutedEventArgs e)
    {
        //This can also be applied for SaveFilePicker.
        var folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "New Project", AllowMultiple = false }
        );

        if (folders.Count == 0)
        {
            Console.WriteLine("No folder selected");
            return;
        }

        var rootDir = new DirectoryInfo(folders[0].Path.AbsolutePath);
        var childDirs = rootDir.GetDirectories();
        var ciDir = childDirs.FirstOrDefault(x => x.Name == ".ci");
        if (ciDir is not null)
        {
            Console.WriteLine("Project already exists");
            return;
        }

        ciDir = new DirectoryInfo(Path.Combine(rootDir.FullName, ".ci"));
        ciDir.Create();

        var project = new Project
        {
            Name = rootDir.Name,
            Location = rootDir.FullName,
            Settings = new ProjectSettings
            {
                VersionControl = VersionControlType.Git,
                GameEngine = GameEngineType.Unity,
            }
        };

        var toml = Toml.FromModel(
            project,
            new TomlModelOptions { IgnoreMissingProperties = true, }
        );
        await File.WriteAllTextAsync(Path.Combine(rootDir.FullName, ".ci", "project.toml"), toml);

        ViewModel.LoadProject(rootDir.FullName);

        Console.WriteLine($"Created new project: {ciDir.FullName}");
    }
}

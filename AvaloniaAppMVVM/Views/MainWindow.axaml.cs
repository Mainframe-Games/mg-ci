using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaAppMVVM.Data;
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

    public void Button_Settings_OnClick(object? sender, RoutedEventArgs e) { }

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

        var rootDir = new DirectoryInfo(folders[0].Path.LocalPath);
        var ciDir = new DirectoryInfo(Path.Combine(rootDir.FullName, ".ci"));

        // if no proj found, create new
        if (!ciDir.Exists)
            CreateNewProject(rootDir);

        // good to go
        ViewModel.LoadCurrentProject(rootDir.FullName);
    }

    private static void CreateNewProject(FileSystemInfo rootDir)
    {
        var project = new Project
        {
            Location = rootDir.FullName,
            Settings = new ProjectSettings { ProjectName = rootDir.Name }
        };

        var toml = Toml.FromModel(
            project,
            new TomlModelOptions { IgnoreMissingProperties = true, }
        );

        var ciDir = new DirectoryInfo(Path.Combine(rootDir.FullName, ".ci"));
        ciDir.Create();

        File.WriteAllText(Path.Combine(rootDir.FullName, ".ci", "project.toml"), toml);
    }
}

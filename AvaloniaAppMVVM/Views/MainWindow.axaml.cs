using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaApp;
using FluentAvalonia.UI.Windowing;

namespace AvaloniaAppMVVM.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();

        // TitleBar.ExtendsContentIntoTitleBar = true;
        // TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

        // SplashScreen = new ComplexSplashScreen();
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
        Console.WriteLine($"Loading project: {rootDir.FullName}");
    }

    public void Button_Settings_OnClick(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Show();
    }
}

using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace AvaloniaApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // SideBar.Children.Add(
        //     new Label
        //     {
        //         Content = "PROJECT NAME",
        //         FontWeight = FontWeight.Heavy,
        //         HorizontalAlignment = HorizontalAlignment.Center
        //     }
        // );
        // SideBar.Children.Add(new Button { Content = "Project Settings" });
        // SideBar.Children.Add(new Button { Content = "Build Config" });
        // SideBar.Children.Add(new Button { Content = "Build Target Configs" });
        //
        // MainContent.Children.Add(new Label { Content = "Hello World!" });
        // MainContent.Children.Add(new Label { Content = "Hello World 2!" });
    }

    private void Button_Github_OnClick(object? sender, RoutedEventArgs e)
    {
        const string url = "https://github.com/Mainframe-Games/mg-ci";
        Process.Start("explorer", url);
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

    private void Button_Settings_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow();
        window.Show(this);
    }

    private void Button_Pane_OnClick(object? sender, RoutedEventArgs e)
    {
        SideBarPane.IsPaneOpen = !SideBarPane.IsPaneOpen;
    }
}

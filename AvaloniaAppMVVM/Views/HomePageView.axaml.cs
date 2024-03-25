using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.ViewModels;
using LoadingIndicators.Avalonia;

namespace AvaloniaAppMVVM.Views;

public class ProcessesTemplate
{
    public IProcess Process { get; set; }
}

public partial class HomePageView : MyUserControl<HomePageViewModel>
{
    private bool _isBuilding;

    private readonly List<ProcessesTemplate> _processes =
    [
        new ProcessesTemplate { Process = new CiProcess { Id = "PreBuild" } },
        new ProcessesTemplate { Process = new CiProcess { Id = "Build" } },
        new ProcessesTemplate { Process = new CiProcess { Id = "Deploy" } },
        new ProcessesTemplate { Process = new CiProcess { Id = "Hooks" } }
    ];

    public HomePageView()
    {
        InitializeComponent();
    }

    protected override void OnInit()
    {
        _viewModel.Project = _project;
        BuildView();
    }

    protected override void OnPreSave() { }

    #region Build View

    private void BuildView()
    {
        var g = new Grid();
        g.ColumnDefinitions = new ColumnDefinitions("Auto *");
        for (var i = 0; i < _processes.Count; i++)
        {
            var process = _processes[i];
            var expander = new Expander
            {
                Name = $"ProcessExpander_{i}",
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 10, 0, 0),
                CornerRadius = new CornerRadius(15),
                Header = BuildGridHeader(process, i),
                Content = new TextBox
                {
                    Name = $"Logs_{i}",
                    Text = "Logs...",
                    IsReadOnly = true,
                    MaxHeight = 500,
                }
            };

            ProcessContainer.Children.Add(expander);
        }
    }

    private Grid BuildGridHeader(ProcessesTemplate process, int i)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto *") };

        // Icons
        // busy
        grid.Children.Add(
            new LoadingIndicator
            {
                Mode = LoadingIndicatorMode.Arc,
                SpeedRatio = 1.2,
                IsVisible = false
            }
        );
        // queued
        grid.Children.Add(
            new LoadingIndicator
            {
                Mode = LoadingIndicatorMode.ThreeDots,
                SpeedRatio = 0,
                IsVisible = false
            }
        );
        // success
        grid.Children.Add(
            new PathIcon
            {
                Foreground = Brushes.Green,
                Data = (Geometry)Application.Current!.Resources["checkmark_regular"]!,
                IsVisible = true
            }
        );
        //failed
        grid.Children.Add(
            new PathIcon
            {
                Foreground = Brushes.Green,
                Data = (Geometry)Application.Current!.Resources["error_circle_regular"]!,
                IsVisible = true
            }
        );

        // Text
        var textStack = new StackPanel { Orientation = Orientation.Horizontal };
        textStack.Children.Add(
            new TextBlock
            {
                Text = process.Process.Id,
                Margin = new Thickness(30, 0, 0, 0),
                FontSize = 20,
                FontWeight = FontWeight.Black,
                VerticalAlignment = VerticalAlignment.Center
            }
        );
        textStack.Children.Add(
            new TextBlock
            {
                Text = process.Process.TotalTime,
                Margin = new Thickness(30, 0, 0, 0),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            }
        );
        grid.Children.Add(textStack);
        Grid.SetColumn(textStack, 1);

        return grid;
    }

    #endregion


    #region Build Process

    private void Button_StartBuild_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isBuilding)
        {
            Console.WriteLine("Build already in progress");
            return;
        }

        _isBuilding = true;
        Dispatcher.UIThread.Post(() => BuildProcessTask(), DispatcherPriority.Background);
    }

    private void RefreshProcesses()
    {
        // refresh processes
        foreach (var process in _viewModel.Processes)
        {
            process.IsQueued = false;
            process.Failed = false;
            process.Succeeded = false;
            process.IsBusy = false;
            process.Logs = string.Empty;
        }
    }

    private async Task BuildProcessTask()
    {
        Console.WriteLine("Start build Task");
        TestStatus.Text = "Building...";

        // refresh processes
        RefreshProcesses();

        // do builds
        foreach (var process in _viewModel.Processes)
        {
            process.IsBusy = true;
            var startTime = DateTime.Now;

            // doing busy things
            while (DateTime.Now - startTime < TimeSpan.FromSeconds(2))
            {
                var log = $"[{DateTime.Now:T}] Building {process.Id}...";
                Console.WriteLine(log);
                process.Logs += log + "\n";
                await Task.Delay(300);
            }

            process.IsBusy = false;
            process.Succeeded = true;
        }

        Console.WriteLine("Done");
        TestStatus.Text = "Done";
    }

    #endregion
}

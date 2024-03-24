using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.ViewModels;

namespace AvaloniaAppMVVM.Views;

public partial class HooksView : UserControl
{
    private HooksViewModel ViewModel => (HooksViewModel)DataContext;

    private Project? _project;

    public HooksView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _project = ViewLocator.GetViewModel<MainWindowViewModel>().CurrentProject;
        var hooks = _project?.Hooks;

        if (hooks is null)
            return;

        foreach (var hook in hooks)
        {
            ViewModel.Items.Add(
                new HookItemTemplate
                {
                    Title = hook.Title,
                    Url = hook.Url,
                    IsErrorChannel = hook.IsErrorChannel
                }
            );
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (_project is null)
            return;

        _project.Hooks.Clear();
        foreach (var item in ViewModel.Items)
        {
            _project.Hooks.Add(
                new HookItemTemplate
                {
                    Title = item.Title,
                    Url = item.Url,
                    IsErrorChannel = item.IsErrorChannel
                }
            );
        }

        _project.Save();
    }

    private void Button_Delete_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: HookItemTemplate item })
        {
            ViewModel.DeleteItem(item);
        }
    }
}

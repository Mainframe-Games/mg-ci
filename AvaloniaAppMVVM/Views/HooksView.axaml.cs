using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.ViewModels;

namespace AvaloniaAppMVVM.Views;

public partial class HooksView : MyUserControl<HooksViewModel>
{
    public HooksView()
    {
        InitializeComponent();
    }

    private void Button_Delete_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: HookItemTemplate item })
            _viewModel.DeleteItem(item);
    }

    protected override void OnInit()
    {
        var hooks = _project.Hooks;

        foreach (var hook in hooks)
        {
            _viewModel.Items.Add(
                new HookItemTemplate
                {
                    Title = hook.Title,
                    Url = hook.Url,
                    IsErrorChannel = hook.IsErrorChannel
                }
            );
        }
    }

    protected override void OnPreSave()
    {
        _project.Hooks.Clear();
        foreach (var item in _viewModel.Items)
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
    }
}

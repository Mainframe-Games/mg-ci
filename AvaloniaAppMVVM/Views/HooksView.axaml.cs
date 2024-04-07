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
        _viewModel.Project = _project;
        foreach (var hook in _project.Hooks)
            _viewModel.Hooks.Add(hook);
    }

    protected override void OnPreSave()
    {
        _project.Hooks = new List<HookItemTemplate>(_viewModel.Hooks);
    }
}

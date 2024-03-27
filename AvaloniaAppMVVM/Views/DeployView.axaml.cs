using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.ViewModels;

namespace AvaloniaAppMVVM.Views;

public partial class DeployView : MyUserControl<DeployViewModel>
{
    public DeployView()
    {
        InitializeComponent();
    }

    private void Button_DeleteSteamVdf_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: StringWrap vdf })
            return;

        if (DataContext is DeployViewModel vm)
            vm.SteamVdfs.Remove(vdf);
    }

    protected override void OnInit()
    {
        _viewModel.Project = _project;
        
        foreach (var steamVdf in _project.Deployment.SteamVdfs)
            _viewModel.SteamVdfs.Add(new StringWrap(steamVdf));
    }

    protected override void OnPreSave()
    {
        _project.Deployment.SteamVdfs.Clear();
        foreach (var steamVdf in _viewModel.SteamVdfs)
            _project.Deployment.SteamVdfs.Add(steamVdf.Value);
    }
}

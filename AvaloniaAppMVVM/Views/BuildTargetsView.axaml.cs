using Avalonia.Interactivity;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.ViewModels;

namespace AvaloniaAppMVVM.Views;

public partial class BuildTargetsView : MyUserControl<BuildTargetsViewModel>
{
    public BuildTargetsView()
    {
        InitializeComponent();
    }

    protected override void OnInit()
    {
        foreach (var buildTarget in _project.BuildTargets)
            _viewModel.BuildTargets.Add(buildTarget);
    }

    protected override void OnPreSave()
    {
        _project.BuildTargets.Clear();
        foreach (var template in _viewModel.BuildTargets)
        {
            _project.BuildTargets.Add(template);
        }
    }

    private void Button_NewTarget_OnClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.BuildTargets.Add(new UnityBuildTarget());
    }

    private void Button_AddScene_OnClick(object? sender, RoutedEventArgs e)
    {
        // _viewModel.SelectedBuildTarget?.Scenes.Add(new StringWrap(string.Empty));
    }

    private void Button_DeleteScene_OnClick(object? sender, RoutedEventArgs e)
    {
        // _viewModel.SelectedBuildTarget?.Scenes.Remove(new StringWrap(string.Empty));
    }

    private void Button_AddExtraScriptingDefine_OnClick(object? sender, RoutedEventArgs e)
    {
        // _viewModel.SelectedBuildTarget?.ExtraScriptingDefines.Add(new StringWrap(string.Empty));
    }

    private void Button_DeleteExtraScriptingDefine_OnClick(object? sender, RoutedEventArgs e)
    {
        // _viewModel.SelectedBuildTarget?.ExtraScriptingDefines.Remove(new StringWrap(string.Empty));
    }
}

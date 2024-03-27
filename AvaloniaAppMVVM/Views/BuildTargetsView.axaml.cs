using Avalonia.Interactivity;
using AvaloniaAppMVVM.Data.Shared;
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
        _viewModel.Project = _project;

        for (var i = 0; i < _project.BuildTargets.Count; i++)
        {
            var buildTarget = _project.BuildTargets[i];
            var template = new UnityBuildTargetTemplate(buildTarget);
            _viewModel.BuildTargets.Add(template);

            // select first one
            if (i == 0)
                _viewModel.SelectedBuildTarget = template;
        }
    }

    protected override void OnPreSave()
    {
        _project.BuildTargets.Clear();
        foreach (var template in _viewModel.BuildTargets)
            _project.BuildTargets.Add(template.Data);
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

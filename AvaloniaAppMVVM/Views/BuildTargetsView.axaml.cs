using Avalonia.Controls;
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
            _viewModel.BuildTargets.Add(buildTarget as UnityBuildTargetTemplate);
    }

    protected override void OnPreSave()
    {
        _project.BuildTargets = new List<BuildTargetTemplate>(_viewModel.BuildTargets);
    }

    private void Button_NewTarget_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BuildTargetsViewModel vm)
        {
            vm.BuildTargets.Add(new UnityBuildTargetTemplate());
        }
    }

    private void Button_AddScene_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button && DataContext is BuildTargetsViewModel vm)
        {
            vm.SelectedBuildTarget?.Scenes.Add("");
        }
    }

    private void Button_DeleteScene_OnClick(object? sender, RoutedEventArgs e)
    {
        if (
            sender is Button { DataContext: string scene }
            && DataContext is BuildTargetsViewModel vm
        )
        {
            vm.SelectedBuildTarget?.Scenes.Remove(scene);
        }
    }

    private void Button_AddExtraScriptingDefine_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button && DataContext is BuildTargetsViewModel vm)
        {
            vm.SelectedBuildTarget?.ExtraScriptingDefines.Add("");
        }
    }

    private void Button_DeleteExtraScriptingDefine_OnClick(object? sender, RoutedEventArgs e)
    {
        if (
            sender is Button { DataContext: string define }
            && DataContext is BuildTargetsViewModel vm
        )
        {
            vm.SelectedBuildTarget?.ExtraScriptingDefines.Remove(define);
        }
    }
}

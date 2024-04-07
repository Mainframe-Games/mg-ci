using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaAppMVVM.Data;
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

    private async void Button_AddScene_OnClick(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is not Window window)
            return;

        var location = Path.Combine(_project.Location!, "Assets");

        var options = new FilePickerOpenOptions
        {
            Title = "Add Scene",
            AllowMultiple = true,
            SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(
                location
            ),
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("unity") { Patterns = ["*.unity"] }
            }
        };

        var files = await window.StorageProvider.OpenFilePickerAsync(options);

        if (files.Count == 0)
            return;

        var buildTarget = _viewModel.SelectedBuildTarget!.Data;

        foreach (var file in files)
        {
            var scenePath = file.Path.LocalPath;
            var sceneName = scenePath.Replace(
                _project.Location! + Path.DirectorySeparatorChar,
                string.Empty
            );

            if (!buildTarget.Scenes.Contains(sceneName))
                buildTarget.Scenes.Add(sceneName);
        }

        _project.Save();
    }

    private void Button_DeleteScene_OnClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Button { DataContext: string str })
            return;

        var buildTarget = _viewModel.SelectedBuildTarget!.Data;

        if (buildTarget.Scenes.Remove(str))
            _project.Save();
    }

    private void Button_AddExtraScriptingDefine_OnClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.SelectedBuildTarget?.Data.ExtraScriptingDefines.Add(string.Empty);
        _project.Save();
    }

    private void Button_DeleteExtraScriptingDefine_OnClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Button { DataContext: string str })
            return;

        _viewModel.SelectedBuildTarget?.Data.ExtraScriptingDefines.Remove(str);
        _project.Save();
    }

    private void Button_AddNewTarget_OnClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.NewTargetCommand("New");
    }
}

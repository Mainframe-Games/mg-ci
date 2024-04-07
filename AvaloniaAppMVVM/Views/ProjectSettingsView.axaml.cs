using Avalonia.Controls;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.ViewModels;

namespace AvaloniaAppMVVM.Views;

public partial class ProjectSettingsView : MyUserControl<ProjectSettingsViewModel>
{
    public ProjectSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnInit()
    {
        _viewModel.Project = _project;

        RefreshVersionControlSettings();
    }

    protected override void OnPreSave() { }

    private void RefreshVersionControlSettings()
    {
        GitSettingsStackPanel.IsVisible =
            _viewModel.Project?.Settings.VersionControl == VersionControlType.Git;
        PlasticSettingsStackPanel.IsVisible =
            _viewModel.Project?.Settings.VersionControl == VersionControlType.Plastic;
    }

    private void SelectingItemsControl_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e
    )
    {
        RefreshVersionControlSettings();
    }
}

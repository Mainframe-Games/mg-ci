using Avalonia.Controls;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.ViewModels;
using ServerClientShared;

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

    protected override void OnPreSave()
    {
    }

    private void RefreshVersionControlSettings()
    {
        var isGit = _viewModel.Project?.Settings.VersionControl == VersionControlType.Git;
        
        GitSettingsStackPanel.IsVisible = isGit;
        PlasticSettingsStackPanel.IsVisible = _viewModel.Project?.Settings.VersionControl == VersionControlType.Plastic;  
        
        if (isGit && !string.IsNullOrEmpty(_project.Settings.GitRepositoryUrl))
        {
            var branches = Git.GetBranchesFromRemote(_project.Settings.GitRepositoryUrl);
            BranchComboBox.ItemsSource = branches;
        }
        
        BranchComboBox.SelectedItem = _viewModel.Project?.Settings.Branch;
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshVersionControlSettings();
    }
}

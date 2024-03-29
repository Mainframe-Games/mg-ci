using Avalonia.Controls;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.ViewModels;
using LibGit2Sharp;

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
            SetGitBranchComboBoxItems();
        }
        
        BranchComboBox.SelectedItem = _viewModel.Project?.Settings.Branch;
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshVersionControlSettings();
    }

    private void SetGitBranchComboBoxItems()
    {
        using var repo = new Repository(_project.Location);
        var branches = repo.Branches
            .Where(x => x.IsRemote && !x.FriendlyName.Contains("HEAD"))
            .Select(x => x.FriendlyName.Replace(x.RemoteName, string.Empty).Trim('/'))
            .ToList();
        
        // var branches = Git.GetBranchesFromRemote(_project.Settings.GitRepositoryUrl);
        BranchComboBox.ItemsSource = branches;
    }
}

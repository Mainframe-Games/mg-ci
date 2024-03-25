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
        _viewModel.ProjectName = _project.Settings.ProjectName;
        _viewModel.Location = _project.Location;
        _viewModel.VersionControl = _project.Settings.VersionControl.ToString();
        _viewModel.GameEngine = _project.Settings.GameEngine.ToString();
        _viewModel.StoreUrl = _project.Settings.StoreUrl;
        _viewModel.StoreThumbnailUrl = _project.Settings.StoreThumbnailUrl;
        _viewModel.LastSuccessfulBuild = _project.Settings.LastSuccessfulBuild;
    }

    protected override void OnPreSave()
    {
        _project.Settings.ProjectName = _viewModel.ProjectName;
        _project.Settings.StoreUrl = _viewModel.StoreUrl;
        _project.Settings.VersionControl = Enum.Parse<VersionControlType>(
            _viewModel.VersionControl
        );
        _project.Settings.GameEngine = Enum.Parse<GameEngineType>(_viewModel.GameEngine);
        _project.Settings.StoreThumbnailUrl = _viewModel.StoreThumbnailUrl;
        _project.Settings.LastSuccessfulBuild = _viewModel.LastSuccessfulBuild;
    }
}

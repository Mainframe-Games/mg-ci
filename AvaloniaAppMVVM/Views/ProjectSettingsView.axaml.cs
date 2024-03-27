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
    }

    protected override void OnPreSave()
    {
    }
}

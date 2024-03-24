using AvaloniaAppMVVM.ViewModels;

namespace AvaloniaAppMVVM.Views;

public partial class PrebuildView : MyUserControl<PrebuildViewModel>
{
    public PrebuildView()
    {
        InitializeComponent();
    }

    protected override void OnInit()
    {
        _viewModel.BuildNumberStandalone = _project.Prebuild.BuildNumberStandalone;
        _viewModel.BuildNumberIphone = _project.Prebuild.BuildNumberIphone;
        _viewModel.AndroidVersionCode = _project.Prebuild.AndroidVersionCode;
    }

    protected override void OnPreSave()
    {
        _project.Prebuild.BuildNumberStandalone = _viewModel.BuildNumberStandalone;
        _project.Prebuild.BuildNumberIphone = _viewModel.BuildNumberIphone;
        _project.Prebuild.AndroidVersionCode = _viewModel.AndroidVersionCode;
    }
}

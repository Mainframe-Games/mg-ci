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
        _viewModel.Project = _project;
    }

    protected override void OnPreSave()
    {
    }
}

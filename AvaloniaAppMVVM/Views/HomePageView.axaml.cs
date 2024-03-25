using AvaloniaAppMVVM.ViewModels;

namespace AvaloniaAppMVVM.Views;

public partial class HomePageView : MyUserControl<HomePageViewModel>
{
    public HomePageView()
    {
        InitializeComponent();
    }

    protected override void OnInit()
    {
        _viewModel.Project = _project;
    }

    protected override void OnPreSave() { }
}

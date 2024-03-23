using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaAppMVVM.ViewModels;

public partial class NewProjectWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _projectLocation = string.Empty;

    [ObservableProperty]
    private string _projectName = string.Empty;
}

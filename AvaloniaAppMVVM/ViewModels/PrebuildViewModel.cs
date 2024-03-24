using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaAppMVVM.ViewModels;

public partial class PrebuildViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _buildNumberStandalone = true;

    [ObservableProperty]
    private bool _buildNumberIphone;

    [ObservableProperty]
    private bool _androidVersionCode = true;
}

using AvaloniaAppMVVM.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaAppMVVM.ViewModels;

public partial class PrebuildViewModel : ViewModelBase
{
    [ObservableProperty] private Project? _project;
}
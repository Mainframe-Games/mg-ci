using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaAppMVVM.ViewModels;

public partial class DeployViewModel : ViewModelBase
{
    public ObservableCollection<StringWrap> SteamVdfs { get; } = [];
    
    [ObservableProperty]
    private Project? _project;

    [RelayCommand]
    public void AddSteamVdf()
    {
        SteamVdfs.Add(new StringWrap("file:///.vdf"));
    }
}

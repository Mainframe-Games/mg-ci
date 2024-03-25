using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaAppMVVM.ViewModels;

public partial class DeployViewModel : ViewModelBase
{
    public ObservableCollection<StringWrap> SteamVdfs { get; } = [];

    [ObservableProperty]
    private bool _appleStore;

    [ObservableProperty]
    private bool _googleStore;

    [ObservableProperty]
    private bool _clanforge;

    [ObservableProperty]
    private bool _awsS3;

    [RelayCommand]
    public void AddSteamVdf()
    {
        SteamVdfs.Add(new StringWrap("file:///.vdf"));
    }
}

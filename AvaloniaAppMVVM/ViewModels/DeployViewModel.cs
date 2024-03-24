using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaAppMVVM.ViewModels;

public partial class DeployViewModel : ViewModelBase
{
    public ObservableCollection<SteamVdf> SteamVdfs { get; } = [];

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
        SteamVdfs.Add(new SteamVdf("file:///.vdf"));
    }
}

public class SteamVdf
{
    public string Value { get; set; } = "file:///.vdf";

    public SteamVdf(string value)
    {
        Value = value;
    }
}

using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaAppMVVM.ViewModels;

public partial class DeployViewModel : ViewModelBase
{
    public ObservableCollection<AppBuildTemplate> AppBuilds { get; } = [];

    [ObservableProperty]
    private Project? _project;

    [RelayCommand]
    public void AddAppBuild()
    {
        AppBuilds.Add(new AppBuildTemplate());
    }

    [RelayCommand]
    public void RemoveAppBuild(AppBuildTemplate template)
    {
        AppBuilds.Add(template);
    }

    [RelayCommand]
    public void AddDepotId(string appId)
    {
        foreach (var appBuild in Project!.Deployment.SteamAppBuilds)
            if (appBuild.AppID == appId)
                appBuild.Depots.Add(new Depot());
    }

    // [RelayCommand]
    // public void RemoveDepotId(string appId, string depotId)
    // {
    //     foreach (var appBuild in Project!.Deployment.SteamVdfs)
    //         if (appBuild.AppID == appId)
    //             appBuild.DepotIds.Remove(depotId);
    // }
}

public class AppBuildTemplate
{
    public string AppId { get; set; } = string.Empty;
    public List<string> DepotIds { get; set; } = [];
}

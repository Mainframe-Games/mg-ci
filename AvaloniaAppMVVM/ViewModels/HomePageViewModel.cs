using System.Diagnostics;
using Avalonia.Media.Imaging;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaAppMVVM.ViewModels;

public partial class HomePageViewModel : ViewModelBase
{
    [ObservableProperty]
    private Project? _project;

    public Task<Bitmap?> ImageSourceBitmapWeb =>
        ImageLoader.LoadFromWeb(
            "https://cdn.cloudflare.steamstatic.com/steam/apps/1622570/header.jpg?t=1693947196"
        );

    [RelayCommand]
    private void OpenStoreLinkCommand()
    {
        Process.Start("explorer", _project.Settings.StoreUrl);
    }
}

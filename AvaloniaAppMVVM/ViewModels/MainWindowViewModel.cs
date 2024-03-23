using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AvaloniaApp;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaAppMVVM.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private ViewModelBase _currentPage = new HomePageViewModel();

    [ObservableProperty]
    private ListItemTemplate? _selectedListItem;

    public ObservableCollection<ListItemTemplate> Items { get; } =
        [
            new ListItemTemplate(typeof(HomePageViewModel), "HomeRegular"),
            new ListItemTemplate(typeof(ButtonsPageViewModel), "CursorHoverRegular"),
        ];

    [RelayCommand]
    public void TogglePaneCommand()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    partial void OnSelectedListItemChanged(ListItemTemplate? value)
    {
        if (value is null)
            return;

        var instance = Design.IsDesignMode
            ? Activator.CreateInstance(value.ModelType)
            : Ioc.Default.GetService(value.ModelType);

        if (instance is null)
            return;

        CurrentPage = (ViewModelBase)instance;
    }

    [RelayCommand]
    public void Button_Github_OnClick()
    {
        const string url = "https://github.com/Mainframe-Games/mg-ci";
        Process.Start("explorer", url);
    }
}

public class ListItemTemplate
{
    public string Label { get; set; }
    public Type ModelType { get; set; }
    public StreamGeometry Icon { get; set; }

    public ListItemTemplate(Type modelType, string iconKey)
    {
        ModelType = modelType;
        Label = modelType.Name.Replace("PageViewModel", string.Empty);

        Application.Current!.TryGetResource(iconKey, out var res);
        Icon = (StreamGeometry)res!;
    }
}

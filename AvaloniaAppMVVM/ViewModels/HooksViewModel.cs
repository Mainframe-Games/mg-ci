using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaAppMVVM.ViewModels;

public partial class HooksViewModel : ViewModelBase
{
    [ObservableProperty] private Project? _project;

    public ObservableCollection<HookItemTemplate> Hooks { get; } = [];
    
    [RelayCommand]
    public void NewHookCommand()
    {
        Hooks.Add(new HookItemTemplate());
    }

    public void DeleteItem(HookItemTemplate item)
    {
        Hooks.Remove(item);
    }
}

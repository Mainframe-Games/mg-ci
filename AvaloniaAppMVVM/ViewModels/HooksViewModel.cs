using System.Collections.ObjectModel;
using AvaloniaAppMVVM.Data;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaAppMVVM.ViewModels;

public partial class HooksViewModel : ViewModelBase
{
    public ObservableCollection<HookItemTemplate> Items { get; } = [];

    [RelayCommand]
    public void NewHookCommand()
    {
        Items.Add(new HookItemTemplate());
    }

    public void DeleteItem(HookItemTemplate item)
    {
        Items.Remove(item);
    }
}

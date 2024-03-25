using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.ViewModels;

namespace AvaloniaAppMVVM.Views;

public abstract class MyUserControl<T> : UserControl
    where T : ViewModelBase
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    protected T _viewModel;
    protected Project _project;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    protected abstract void OnInit();
    protected abstract void OnPreSave();

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _viewModel = DataContext as T ?? throw new NullReferenceException();
        _project = ViewLocator.GetViewModel<MainWindowViewModel>().CurrentProject ?? new Project();
        OnInit();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        OnPreSave();
        _project.Save();
    }
}

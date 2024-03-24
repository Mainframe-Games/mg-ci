using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.ViewModels;

namespace AvaloniaAppMVVM.Views;

public abstract class MyUserControl<T> : UserControl
    where T : ViewModelBase
{
    protected T _viewModel;
    protected Project _project;

    protected abstract void OnInit();
    protected abstract void OnPreSave();

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _viewModel = DataContext as T ?? throw new NullReferenceException();

        _project =
            ViewLocator.GetViewModel<MainWindowViewModel>().CurrentProject
            ?? throw new NullReferenceException();

        OnInit();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        OnPreSave();
        _project.Save();
    }
}

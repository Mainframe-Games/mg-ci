using Avalonia.Controls;
using Avalonia.Controls.Templates;
using AvaloniaAppMVVM.ViewModels;
using AvaloniaAppMVVM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace AvaloniaAppMVVM;

public class ViewLocator : IDataTemplate
{
    private readonly Dictionary<Type, Func<Control?>> _locator = new();

    public ViewLocator()
    {
        RegisterViewFactory<MainWindowViewModel, MainWindow>();
        RegisterViewFactory<HomePageViewModel, HomePageView>();
        RegisterViewFactory<ButtonsPageViewModel, ButtonsPageView>();
    }

    public Control Build(object? data)
    {
        if (data is null)
            return new TextBlock { Text = "No VM provided" };

        _locator.TryGetValue(data.GetType(), out var factory);
        return factory?.Invoke() ?? new TextBlock { Text = $"VM Not Registered: {data.GetType()}" };
    }

    public bool Match(object? data)
    {
        return data is ObservableObject;
    }

    private void RegisterViewFactory<TViewModel, TView>()
        where TViewModel : class
        where TView : Control =>
        _locator.Add(
            typeof(TViewModel),
            Design.IsDesignMode ? Activator.CreateInstance<TView> : Ioc.Default.GetService<TView>
        );

    public static ViewModelBase GetViewModel(Type type)
    {
        var vm = Design.IsDesignMode
            ? Activator.CreateInstance(type)
            : Ioc.Default.GetService(type);

        return vm as ViewModelBase ?? throw new NullReferenceException();
    }

    public static T GetViewModel<T>()
        where T : ViewModelBase
    {
        var instance = Design.IsDesignMode
            ? Activator.CreateInstance<T>()
            : Ioc.Default.GetService<T>();

        return instance ?? throw new NullReferenceException();
    }
}

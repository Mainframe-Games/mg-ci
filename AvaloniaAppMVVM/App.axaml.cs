using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaApp;
using AvaloniaAppMVVM.ViewModels;
using AvaloniaAppMVVM.Views;
using CommunityToolkit.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace AvaloniaAppMVVM;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var locator = new ViewLocator();
        DataTemplates.Add(locator);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            ConfigureViewModels(services);
            ConfigureViews(services);
            var provider = services.BuildServiceProvider();

            Ioc.Default.ConfigureServices(provider);

            var vm = Ioc.Default.GetService<MainWindowViewModel>();
            var b = locator.Build(vm);
            var view = (Window)b;
            view.DataContext = vm;

            desktop.MainWindow = view;
        }

        base.OnFrameworkInitializationCompleted();
    }

    [Singleton(typeof(MainWindowViewModel))]
    [Transient(typeof(HomePageViewModel))]
    [Transient(typeof(ButtonsPageViewModel))]
    internal static partial void ConfigureViewModels(IServiceCollection services);

    [Singleton(typeof(MainWindow))]
    [Singleton(typeof(SettingsWindow))]
    [Transient(typeof(HomePageView))]
    [Transient(typeof(ButtonsPageView))]
    internal static partial void ConfigureViews(IServiceCollection services);
}

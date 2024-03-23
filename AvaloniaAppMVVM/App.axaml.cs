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
            var view = (Window)locator.Build(vm);
            view.DataContext = vm;

            desktop.MainWindow = view;
        }

        base.OnFrameworkInitializationCompleted();
    }

    [Singleton(typeof(MainWindowViewModel))]
    [Singleton(typeof(NewProjectWindowViewModel))]
    [Singleton(typeof(SettingsWindowViewModel))]
    [Transient(typeof(HomePageViewModel))]
    [Transient(typeof(ProjectSettingsViewModel))]
    [Transient(typeof(PrebuildViewModel))]
    [Transient(typeof(BuildTargetsViewModel))]
    [Transient(typeof(DeployViewModel))]
    [Transient(typeof(HooksViewModel))]
    internal static partial void ConfigureViewModels(IServiceCollection services);

    [Singleton(typeof(MainWindow))]
    [Singleton(typeof(NewProjectWindow))]
    [Singleton(typeof(SettingsWindow))]
    [Transient(typeof(HomePageView))]
    [Transient(typeof(ProjectSettingsView))]
    [Transient(typeof(PrebuildView))]
    [Transient(typeof(BuildTargetsView))]
    [Transient(typeof(DeployView))]
    [Transient(typeof(HooksView))]
    internal static partial void ConfigureViews(IServiceCollection services);
}

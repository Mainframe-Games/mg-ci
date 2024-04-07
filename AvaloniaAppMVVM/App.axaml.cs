using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.Services;
using AvaloniaAppMVVM.ViewModels;
using AvaloniaAppMVVM.Views;
using CommunityToolkit.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SocketServer;

namespace AvaloniaAppMVVM;

public partial class App : Application
{
    private static readonly Client _client =
        new(AppSettings.Singleton.ServerIp!, AppSettings.Singleton.ServerPort);

    public static readonly BuildClientService BuildClient = new(_client);

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        AppSettings.Load();
        _client.AddService(BuildClient);
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
    [Transient(typeof(HomePageViewModel))]
    [Transient(typeof(ProjectSettingsViewModel))]
    [Transient(typeof(PrebuildViewModel))]
    [Transient(typeof(BuildTargetsViewModel))]
    [Transient(typeof(DeployViewModel))]
    [Transient(typeof(HooksViewModel))]
    internal static partial void ConfigureViewModels(IServiceCollection services);

    [Singleton(typeof(MainWindow))]
    [Transient(typeof(HomePageView))]
    [Transient(typeof(ProjectSettingsView))]
    [Transient(typeof(PrebuildView))]
    [Transient(typeof(BuildTargetsView))]
    [Transient(typeof(DeployView))]
    [Transient(typeof(HooksView))]
    internal static partial void ConfigureViews(IServiceCollection services);
}

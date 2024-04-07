using AvaloniaAppMVVM.Data;
using FluentAvalonia.UI.Windowing;

namespace AvaloniaAppMVVM.Views;

public partial class AppSettingsView : AppWindow
{
    public AppSettingsView()
    {
        InitializeComponent();
        
        var setting = AppSettings.Singleton;
        ServerIp.Text = setting.ServerIp;
        ServerPort.Text = setting.ServerPort.ToString();

        // GitUsername.Text = setting.GitUsername;
        // GitPassword.Text = setting.GitPassword;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        var setting = AppSettings.Singleton;
        
        setting.ServerIp = ServerIp.Text;
        setting.ServerPort = ushort.TryParse(ServerPort.Text, out var port) ? port: (ushort)8080;
        
        // setting.GitUsername = GitUsername.Text;
        // setting.GitPassword = GitPassword.Text;
        
        AppSettings.Singleton.Save();
    }
}
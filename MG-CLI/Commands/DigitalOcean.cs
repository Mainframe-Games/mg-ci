using System.CommandLine;
using CliWrap;
using CliWrap.Buffered;
using Command = System.CommandLine.Command;

namespace MG_CLI;

public class DigitalOcean : Command
{
    private readonly Argument<string> _ipAddress = new("ip-address")
    {
        HelpName = "The IP address of the DigitalOcean droplet to deploy to"
    };
    private readonly Argument<string> _serviceFilePath = new("service-file-path")
    {
        HelpName = "The path to the systemd service file to deploy. e.g ./master_server.service"
    };
    private readonly Argument<string> _buildPath = new("build-path")
    {
        HelpName = "The path to the build directory on the server. e.g ./builds/master_server"
    };
    private readonly Option<string> _nginxConfigPath = new("--nginx-config", "-n")
    {
        HelpName = "The path to the nginx config file to deploy. e.g ./master_server.conf"
    };
    
    public DigitalOcean() : base("digitalocean", "Manage DigitalOcean resources")
    {
        Add(_ipAddress);
        Add(_serviceFilePath);
        Add(_buildPath);
        Add(_nginxConfigPath);
        SetAction(Run);
    }

    private async Task<int> Run(ParseResult result, CancellationToken token)
    {
        var ip = result.GetRequiredValue(_ipAddress);
        var serviceFilePath = result.GetRequiredValue(_serviceFilePath);
        var nginxFilePath = result.GetValue(_nginxConfigPath);
        var buildPath = result.GetValue(_buildPath);
        
        var serviceName = new FileInfo(serviceFilePath).Name;
        var remoteDirectory = GetValueFromServiceFile(serviceFilePath, "WorkingDirectory");
        var remoteExe = GetValueFromServiceFile(serviceFilePath, "ExecStart");

        try
        {
            // Stop service and remove all files
            await StopSystemCtl(ip, serviceName, remoteDirectory);
            
            // Copy new service file contents if any
            if (!string.IsNullOrWhiteSpace(serviceFilePath))
                await CopySystemCtlContents(ip, serviceFilePath);
        
            // Copy nginx config file to server
            if (!string.IsNullOrWhiteSpace(nginxFilePath))
            {
                await CopyNginxContents(ip, nginxFilePath);
                await Ssh(ip, "sudo nginx -s reload");
            }
        
            // Copy build files
            await Scp(ip, buildPath!,"~", true);
        
            // Restart service
            await Ssh(ip, $"chmod +x \"{remoteExe}\" && sudo systemctl daemon-reload && sudo systemctl enable {serviceName} && sudo systemctl start {serviceName}");
        }
        catch(Exception e)
        {
            Log.PrintError(e.ToString());
            return e.HResult;
        }

        return 0;
    }

    #region Helpers

    private async Task StopSystemCtl(string ip, string serviceName, string remoteDirectory)
    {
        Log.Print("Stopping service on server and deleting old files...");
        // ssh root@{{master_server_ip}} 'sudo systemctl stop master_server || echo "Service not running, continuing..." && rm -rf /root/master_server'
        await Ssh(ip, $"sudo systemctl stop {serviceName} || echo \"Service not running, continuing...\"");
        await Ssh(ip, $"rm -rf {remoteDirectory}");
    }

    private async Task CopySystemCtlContents(string ip, string serviceFilePath)
    {
        // scp .ci/systemd/master_server.service root@{{master_server_ip}}:/etc/systemd/system/master_server.service
        Log.Print($"Copying systemd service file to server... {serviceFilePath}");

        var fileInfo = new FileInfo(serviceFilePath);
        if (fileInfo.Extension != ".service")
            throw new Exception("Service file must have .service extension");

        await Scp(ip, fileInfo.FullName, $"/etc/systemd/system/{fileInfo.Name}");
    }

    private async Task CopyNginxContents(string ip, string serviceFilePath)
    {
        // scp .ci/nginx/master_server.conf root@{{master_server_ip}}:/etc/nginx/conf.d/master_server.conf
        Log.Print($"Copying nginx config file to server... {serviceFilePath}");
        var fileInfo = new FileInfo(serviceFilePath);
        if (fileInfo.Extension != ".conf")
            throw new Exception("Nginx config file must have .conf extension");
        await Scp(ip, fileInfo.FullName, $"/etc/nginx/conf.d/{fileInfo.Name}");
    }
    
    #endregion
    
    private static string GetValueFromServiceFile(in string serviceFilePath, in string key)  
    {
        var lines = File.ReadAllLines(serviceFilePath);
        foreach (var line in lines)
        {
            if (line.StartsWith($"{key}="))
            {
                var path = line.Split("=").Last().Trim();
                return path;
            }
        }
        
        throw new Exception("WorkingDirectory not found in service file");
    }

    #region Wrappers
    
    private async Task Ssh(string ip, string command)
    {
        var res = await Cli
            .Wrap("ssh")
            .WithArguments($"root@{ip} '{command}'")
            .WithCustomPipes()
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
        
        // if (!res.IsSuccess)
            // throw new Exception($"ssh command failed: {res.StandardError}");
    }
    
    private async Task Scp(string ip, string filePath, string locationPath, bool recursive = false)
    {
        var args = recursive ? "-r" : string.Empty;
        var res = await Cli.Wrap("scp")
            .WithArguments($"{args} {filePath} root@{ip}:{locationPath}".Trim())
            .WithCustomPipes()
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
        
        if (!res.IsSuccess)
            throw new Exception($"Scp command failed: {res.StandardError}");
    }
    
    #endregion
}
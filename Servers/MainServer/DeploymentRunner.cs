using System.Text;
using ClanforgeDeployment;
using Deployment.Deployments;
using MainServer.Configs;
using MainServer.Utils;
using SharedLib;
using SteamDeployment;
using UnityServicesDeployment;
using Workspace = MainServer.Workspaces.Workspace;

namespace MainServer;

internal class DeploymentRunner
{
    private readonly List<string>? _steamVdfs;
    private readonly bool _clanforge;
    private readonly bool _appleStore;
    private readonly bool _googleStore;
    private readonly bool _awsS3;

    private readonly Workspace _workspace;
    private readonly string _fullVersion;
    private readonly string[] _changeLogArray;
    private readonly ServerConfig _serverConfig;

    private readonly string _workspaceName;

    public DeploymentRunner(
        Workspace workspace,
        string fullVersion,
        string[] changeLogArray,
        ServerConfig serverConfig
    )
    {
        _workspace = workspace;
        _fullVersion = fullVersion;
        _changeLogArray = changeLogArray;
        _serverConfig = serverConfig;

        _workspaceName = new DirectoryInfo(workspace.ProjectPath).Name;

        var projectToml = workspace.GetProjectToml();
        _steamVdfs = projectToml.GetValue<List<string>>("deployment", "steam_vdfs");
        _clanforge = projectToml.GetValue<bool>("deployment", "clanforge");
        _appleStore = projectToml.GetValue<bool>("deployment", "apple_store");
        _googleStore = projectToml.GetValue<bool>("deployment", "google_store");
        _awsS3 = projectToml.GetValue<bool>("deployment", "aws_s3");
    }

    public async void Deploy()
    {
        try
        {
            // client deploys
            await DeployApple(); // apple first as apple takes longer to process on appstore connect
            await DeployGoogle();
            DeploySteam();

            /*
             * Note: clanforge relies on steam URL, so steam MUST be updated first
             */

            // server deploys
            await DeployToS3Bucket();
            await DeployClanforge();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            // pipeline?.SendErrorHook(e);
        }
    }

    private async Task DeployToS3Bucket()
    {
        if (_serverConfig.S3 == null || !_awsS3)
            return;

        // upload to s3
        var pathToBuild = "TODO: path/to/build";
        var s3 = new AmazonS3Deploy(
            _serverConfig.S3.AccessKey,
            _serverConfig.S3.SecretKey,
            _serverConfig.S3.BucketName
        );
        await s3.DeployAsync(pathToBuild);

        if (_serverConfig.Ugs?.ServerHosting == null)
            return;

        if (_serverConfig.Ugs.ServerHosting.BuildId == 0)
            throw new Exception("Invalid build Id");

        var project = _serverConfig.Ugs.GetProjectFromName(_workspaceName);
        var gameServer = new UnityGameServerRequest(
            _serverConfig.Ugs.KeyId,
            _serverConfig.Ugs.SecretKey
        );
        await gameServer.CreateNewBuildVersion(
            project.ProjectId,
            project.EnvironmentId!,
            _serverConfig.Ugs.ServerHosting.BuildId,
            _serverConfig.S3.Url!,
            _serverConfig.S3.AccessKey!,
            _serverConfig.S3.SecretKey!
        );

        Console.WriteLine("Unity server updated");
    }

    private async Task DeployClanforge()
    {
        if (!_clanforge)
            return;

        Args.Environment.TryGetArg("-setlive", out var beta, _serverConfig.Steam.DefaultSetLive);
        Args.Environment.TryGetArg(
            "-clanforge",
            out var profile,
            _serverConfig.Clanforge.DefaultProfile
        );
        var isFull = Args.Environment.IsFlag("-full");

        var clanforgeConfig = _serverConfig.Clanforge!;
        var AuthToken =
            $"Basic {Base64Key.Generate(clanforgeConfig.AccessKey, clanforgeConfig.SecretKey)}";

        var imageId = clanforgeConfig.GetImageId(profile);
        var url = Uri.EscapeDataString(clanforgeConfig.GetUrl(beta));

        var clanforge = new ClanForgeDeploy(
            AuthToken,
            clanforgeConfig.Asid,
            clanforgeConfig.MachineId,
            imageId,
            isFull,
            _fullVersion,
            url
        );
        await clanforge.Deploy();
    }

    private async Task DeployApple()
    {
        if (!_appleStore)
            return;

        // var macRunner =
        //     BuildRunnerFactory.GetRunner("macos")
        //     ?? throw new NullReferenceException("Mac runner is not found");
        //
        // macRunner.SendJObject(
        //     new JObject
        //     {
        //         ["ProjectGuid"] = project.Guid!,
        //         ["AppleId"] = _config.AppleStore?.AppleId,
        //         ["AppSpecificPassword"] = _config.AppleStore?.AppSpecificPassword
        //     }
        // );
        //
        // var task = new TaskCompletionSource();
        // macRunner.OnStringMessage += message =>
        // {
        //     var obj = JObject.Parse(message);
        //     var status = obj["Status"]?.ToString();
        //
        //     if (status == "Completed")
        //         task.SetResult();
        //     else
        //         throw new Exception($"Xcode deploy failed: {status}");
        // };
        // await task.Task;
    }

    private async Task DeployGoogle()
    {
        if (!_googleStore)
            return;

        // var packageName = _workspace.ProjectSettings.GetValue<string>(
        //     "applicationIdentifier.Android"
        // );
        //
        // var productName = project.Settings.ProjectName;
        // var buildPath = "TODO: get build path from build settings asset";
        // var path = Path.Combine(buildPath, $"{productName}.aab");
        // var aabFile = new FileInfo(path);
        //
        // if (!aabFile.Exists)
        //     throw new FileNotFoundException($"aab file not found: {path}");
        //
        // var googlePlayDeploy = new GooglePlayDeploy(
        //     packageName,
        //     aabFile.FullName,
        //     _serverConfig.GoogleStore!.CredentialsPath!,
        //     _serverConfig.GoogleStore.ServiceUsername!,
        //     _fullVersion,
        //     string.Join("\n", _changeLogArray)
        // );
        // await googlePlayDeploy.Deploy();
    }

    private void DeploySteam()
    {
        if (_steamVdfs == null)
            return;

        if (_serverConfig.Steam is null)
            throw new NullReferenceException("Steam config is null");

        foreach (var vdfPath in _steamVdfs)
        {
            var steam = new SteamDeploy(
                vdfPath,
                _serverConfig.Steam.Password!,
                _serverConfig.Steam.Username!,
                _fullVersion,
                _workspace.SetLive!
            );
            steam.Deploy();
        }
    }

    private string? BuildPipelineOnGetExtraHookLog()
    {
        try
        {
            // if (_config?.Clanforge != null)
            // {
            //     pipeline.Args.TryGetArg(
            //         "-clanforge",
            //         out var profile,
            //         _config.Clanforge.DefaultProfile
            //     );
            //     return _config.Clanforge.BuildHookMessage(profile, "Updated");
            // }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    private static class Base64Key
    {
        public static string Generate(string accessKey, string secretKey)
        {
            var bytes = Encoding.UTF8.GetBytes($"{accessKey}:{secretKey}");
            var base64 = Convert.ToBase64String(bytes);
            return base64;
        }
    }
}

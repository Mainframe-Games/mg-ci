using AvaloniaAppMVVM.Data;
using Deployment.Deployments;
using Deployment.Server.Unity;
using Server.Configs;
using Server.RemoteDeploy;
using SharedLib;
using SteamDeployment;

namespace Server;

public class DeploymentRunner(
    Project project,
    Workspace workspace,
    string fullVersion,
    string[] changeLogArray
)
{
    private readonly List<string>? _steamVdfs = project.Deployment.SteamVdfs;
    private readonly bool _clanforge = project.Deployment.Clanforge;
    private readonly bool _appleStore = project.Deployment.AppleStore;
    private readonly bool _googleStore = project.Deployment.GoogleStore;
    private readonly bool _awsS3 = project.Deployment.AwsS3;

    private readonly ServerConfig _config = ServerConfig.Load();

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
            Logger.Log(e);
            // pipeline?.SendErrorHook(e);
        }
    }

    private async Task DeployToS3Bucket()
    {
        if (_config.S3 == null || !_awsS3)
            return;

        // // upload to s3
        // var pathToBuild = pipeline
        //     .Workspace.GetBuildTarget($"{BuildTargetFlag.Linux64}_Server")
        //     .BuildPath;
        // var s3 = new AmazonS3Deploy(
        //     _config.S3.AccessKey,
        //     _config.S3.SecretKey,
        //     _config.S3.BucketName
        // );
        // await s3.DeployAsync(pathToBuild);
        //
        // if (_config.Ugs?.ServerHosting == null)
        //     return;
        //
        // if (_config.Ugs.ServerHosting.BuildId == 0)
        //     throw new Exception("Invalid build Id");
        //
        // var project = _config.Ugs.GetProjectFromName(workspace.Name);
        // var gameServer = new UnityGameServerRequest(_config.Ugs.KeyId, _config.Ugs.SecretKey);
        // await gameServer.CreateNewBuildVersion(
        //     project.ProjectId,
        //     project.EnvironmentId,
        //     _config.Ugs.ServerHosting.BuildId,
        //     _config.S3.Url,
        //     _config.S3.AccessKey,
        //     _config.S3.SecretKey
        // );
        //
        // Logger.Log("Unity server updated");
    }

    private async Task DeployClanforge()
    {
        if (!_clanforge)
            return;

        Args.Environment.TryGetArg("-setlive", out var beta, _config.Steam.DefaultSetLive);
        Args.Environment.TryGetArg("-clanforge", out var profile, _config.Clanforge.DefaultProfile);
        var isFull = Args.Environment.IsFlag("-full");
        var clanforge = new ClanForgeDeploy(_config.Clanforge, profile, fullVersion, beta, isFull);
        await clanforge.Deploy();
    }

    private async Task DeployApple()
    {
        if (!_appleStore || !OperatingSystem.IsMacOS())
            return;

        var apple = new RemoteAppleDeploy
        {
            WorkspaceName = workspace.Name,
            Config = _config.AppleStore,
        };

        await apple.ProcessAsync();
    }

    private async Task DeployGoogle()
    {
        if (!_googleStore)
            return;

        var packageName = workspace.ProjectSettings.GetValue<string>(
            "applicationIdentifier.Android"
        );

        var changeLog = string.Join("\n", changeLogArray);
        var buildSettingsAsset = workspace.GetBuildTarget(BuildTargetFlag.Android.ToString());
        var productName = buildSettingsAsset.GetValue<string>("ProductName");
        var buildPath = buildSettingsAsset.GetValue<string>("BuildPath");
        var path = Path.Combine(buildPath, $"{productName}.aab");
        var aabFile = new FileInfo(path);

        if (!aabFile.Exists)
            throw new FileNotFoundException($"aab file not found: {path}");

        await GooglePlayDeploy.Deploy(
            packageName,
            aabFile.FullName,
            _config.GoogleStore.CredentialsPath,
            _config.GoogleStore.ServiceUsername,
            fullVersion,
            changeLog
        );
    }

    private void DeploySteam()
    {
        if (_steamVdfs == null)
            return;

        if (_config.Steam is null)
            throw new NullReferenceException("Steam config is null");

        foreach (var vdfPath in _steamVdfs)
        {
            var password = _config.Steam.Password!;
            var username = _config.Steam.Username!;
            var steam = new SteamDeploy(
                vdfPath,
                password,
                username,
                fullVersion,
                project.Deployment.SteamSetLive!
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
            Logger.Log(e);
        }

        return null;
    }
}

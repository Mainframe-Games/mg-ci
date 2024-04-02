using AvaloniaAppMVVM.Data;
using ClanforgeDeployment;
using Deployment.Deployments;
using GooglePlayDeployment;
using Newtonsoft.Json.Linq;
using Server.Configs;
using SharedLib;
using SteamDeployment;
using UnityServicesDeployment;

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

        // upload to s3
        var pathToBuild = "TODO: path/to/build";
        var s3 = new AmazonS3Deploy(
            _config.S3.AccessKey,
            _config.S3.SecretKey,
            _config.S3.BucketName
        );
        await s3.DeployAsync(pathToBuild);

        if (_config.Ugs?.ServerHosting == null)
            return;

        if (_config.Ugs.ServerHosting.BuildId == 0)
            throw new Exception("Invalid build Id");

        var project = _config.Ugs.GetProjectFromName(workspace.Name);
        var gameServer = new UnityGameServerRequest(_config.Ugs.KeyId, _config.Ugs.SecretKey);
        await gameServer.CreateNewBuildVersion(
            project.ProjectId,
            project.EnvironmentId,
            _config.Ugs.ServerHosting.BuildId,
            _config.S3.Url,
            _config.S3.AccessKey,
            _config.S3.SecretKey
        );

        Logger.Log("Unity server updated");
    }

    private async Task DeployClanforge()
    {
        if (!_clanforge)
            return;

        Args.Environment.TryGetArg("-setlive", out var beta, _config.Steam.DefaultSetLive);
        Args.Environment.TryGetArg("-clanforge", out var profile, _config.Clanforge.DefaultProfile);
        var isFull = Args.Environment.IsFlag("-full");

        var clanforgeConfig = _config.Clanforge!;
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
            fullVersion,
            url
        );
        await clanforge.Deploy();
    }

    private async Task DeployApple()
    {
        if (!_appleStore)
            return;

        var macRunner =
            BuildRunnerFactory.GetRunner("macos")
            ?? throw new NullReferenceException("Mac runner is not found");

        macRunner.SendJObject(
            new JObject
            {
                ["ProjectGuid"] = project.Guid!,
                ["AppleId"] = _config.AppleStore?.AppleId,
                ["AppSpecificPassword"] = _config.AppleStore?.AppSpecificPassword
            }
        );

        var task = new TaskCompletionSource();
        macRunner.OnStringMessage += message =>
        {
            var obj = JObject.Parse(message);
            var status = obj["Status"]?.ToString();

            if (status == "Completed")
                task.SetResult();
            else
                throw new Exception($"Xcode deploy failed: {status}");
        };
        await task.Task;
    }

    private async Task DeployGoogle()
    {
        if (!_googleStore)
            return;

        var packageName = workspace.ProjectSettings.GetValue<string>(
            "applicationIdentifier.Android"
        );

        var productName = project.Settings.ProjectName;
        var buildPath = "TODO: get build path from build settings asset";
        var path = Path.Combine(buildPath, $"{productName}.aab");
        var aabFile = new FileInfo(path);

        if (!aabFile.Exists)
            throw new FileNotFoundException($"aab file not found: {path}");

        var googlePlayDeploy = new GooglePlayDeploy(
            packageName,
            aabFile.FullName,
            _config.GoogleStore!.CredentialsPath!,
            _config.GoogleStore.ServiceUsername!,
            fullVersion,
            string.Join("\n", changeLogArray)
        );
        await googlePlayDeploy.Deploy();
    }

    private void DeploySteam()
    {
        if (_steamVdfs == null)
            return;

        if (_config.Steam is null)
            throw new NullReferenceException("Steam config is null");

        foreach (var vdfPath in _steamVdfs)
        {
            var steam = new SteamDeploy(
                vdfPath,
                _config.Steam.Password!,
                _config.Steam.Username!,
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

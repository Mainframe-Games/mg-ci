using System.Text;
using ClanforgeDeployment;
using Deployment.Deployments;
using GooglePlayDeployment;
using MainServer.Configs;
using MainServer.Services.Packets;
using MainServer.Utils;
using SharedLib;
using SteamDeployment;
using Tomlyn.Model;
using UnityBuilder;
using UnityServicesDeployment;
using Workspace = MainServer.Workspaces.Workspace;

namespace MainServer.Deployment;

internal class DeploymentRunner
{
    private readonly List<AppBuild> _steamAppBuilds = [];
    private readonly bool _clanforge;
    private readonly bool _appleStore;
    private readonly bool _googleStore;
    private readonly bool _awsS3;

    private readonly Guid _projectGuid;
    private readonly Workspace _workspace;
    private readonly string _fullVersion;
    private readonly string[] _changeLogArray;
    private readonly ServerConfig _serverConfig;

    private readonly string _workspaceName;
    private readonly string _projectName;

    private readonly string _downloadsPath;

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

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _downloadsPath = Path.Combine(home, "ci-cache", "Downloads");

        var projectToml = workspace.GetProjectToml();
        _projectGuid = new Guid(
            projectToml.GetValue<string>("guid") ?? throw new NullReferenceException()
        );

        _projectName =
            projectToml.GetValue<string>("settings", "project_name")
            ?? throw new NullReferenceException();

        if (projectToml["deployment"] is not TomlTable deployment)
            return;

        // steam depots
        foreach (var appBuild in (TomlTableArray)deployment["steam_app_builds"])
        {
            var depots = new Dictionary<string, Depot>();
            foreach (var depot in (TomlTableArray)appBuild["depots"])
            {
                var id = depot.GetValue<string>("id");
                var buildTarget = depot.GetValue<string>("build_target_name");
                depots.Add(
                    id!,
                    new Depot { FileMapping = new FileMapping { LocalPath = $"/{buildTarget}/*" } }
                );
            }

            var build = new AppBuild
            {
                AppID = appBuild.GetValue<string>("app_id")!,
                Desc = _fullVersion,
                SetLive = "beta",
                ContentRoot = Path.Combine(_downloadsPath, _projectGuid.ToString()),
                BuildOutput = Path.Combine(_downloadsPath, _projectGuid.ToString(), "SteamOutput"),
                Depots = depots,
            };
            _steamAppBuilds.Add(build);
        }

        // deployment options
        _clanforge = projectToml.GetValue<bool>("deployment", "clanforge");
        _appleStore = projectToml.GetValue<bool>("deployment", "apple_store");
        _googleStore = projectToml.GetValue<bool>("deployment", "google_store");
        _awsS3 = projectToml.GetValue<bool>("deployment", "aws_s3");
    }

    public async Task Deploy()
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
            _serverConfig.Ugs.KeyId!,
            _serverConfig.Ugs.SecretKey!
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

        Args.Environment.TryGetArg("-setlive", out var beta, _serverConfig.Steam!.DefaultSetLive!);
        Args.Environment.TryGetArg(
            "-clanforge",
            out var profile,
            _serverConfig.Clanforge!.DefaultProfile!
        );
        var isFull = Args.Environment.IsFlag("-full");

        var clanforgeConfig = _serverConfig.Clanforge!;
        var AuthToken =
            $"Basic {Base64Key.Generate(clanforgeConfig.AccessKey!, clanforgeConfig.SecretKey!)}";

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

        var xcodeService = ClientServicesManager.GetXcodeService();

        var data = new XcodeDeployPacket
        {
            ProjectGuid = _projectGuid,
            AppleId = _serverConfig.AppleStore?.AppleId,
            AppSpecificPassword = _serverConfig.AppleStore?.AppSpecificPassword
        };
        await xcodeService.SendJson(data.ToJson());
    }

    private async Task DeployGoogle()
    {
        if (!_googleStore)
            return;

        var unityProjectSettings = new UnityProjectSettings(_workspace.ProjectPath);
        var packageName = unityProjectSettings.GetValue("applicationIdentifier", "Android");

        var aabPath = Path.Combine(
            _downloadsPath,
            _projectGuid.ToString(),
            "Android",
            $"{_projectName}.aab"
        );
        var aabFile = new FileInfo(aabPath);

        if (!aabFile.Exists)
            throw new FileNotFoundException($".aab file not found: {aabPath}");

        var googlePlayDeploy = new GooglePlayDeploy(
            packageName,
            aabFile.FullName,
            _serverConfig.GoogleStore!.CredentialsPath!,
            _serverConfig.GoogleStore.ServiceUsername!,
            _fullVersion,
            string.Join("\n", _changeLogArray)
        );
        await googlePlayDeploy.Deploy();
    }

    private void DeploySteam()
    {
        if (_steamAppBuilds.Count == 0)
            return;

        if (_serverConfig.Steam is null)
            throw new NullReferenceException("Steam config is null");

        foreach (var appBuild in _steamAppBuilds)
        {
            var steam = new SteamDeploy(
                appBuild,
                _serverConfig.Steam.Password!,
                _serverConfig.Steam.Username!
            );
            steam.Deploy();
            break;
        }
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

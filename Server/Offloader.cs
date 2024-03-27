using Deployment.Configs;
using Server.Endpoints;
using Server.RemoteDeploy;
using SharedLib;
using SharedLib.Build;

namespace Server;

public class Offloader : IOffloadable
{
    // interface
    public string? Url { get; set; }
    public List<BuildTargetFlag>? Targets { get; set; }
    public List<string> PendingIds { get; set; } = new();
    public OffloadServerPacket? Packet { get; set; }

    // class
    public string? SendBackUrl { get; set; }
    public string? WorkspaceName { get; set; }
    public string? WorkspaceBranch { get; set; }
    public string? ProjectId { get; set; }

    // offload builds
    public BuildConfig? BuildConfig { get; set; }

    // offload deploys
    public XcodeConfig? XcodeConfig { get; set; }

    public bool IsOffload(
        BuildSettingsAsset build,
        BuildVersions? buildVersion,
        int changesetId,
        bool isClean
    )
    {
        var flag = build.GetBuildTargetFlag();

        if (Targets?.Contains(flag) is not true)
            return false;

        Packet ??= new OffloadServerPacket
        {
            WorkspaceName = WorkspaceName,
            ChangesetId = changesetId,
            BuildVersion = buildVersion,
            PipelineId = ProjectId,
            CleanBuild = isClean,
            Branch = WorkspaceBranch,
            Builds = new Dictionary<string, OffloadBuildConfig>()
        };

        CreateRemoteBuildTargetConfig(Packet, build, flag);
        return true;
    }

    /// <summary>
    /// Returns once buildIds count is 0
    /// </summary>
    public async Task WaitBuildIdsAsync()
    {
        var cachedCount = -1;
        while (PendingIds.Count > 0)
        {
            // to limit the amount of log spamming just log when count changes
            if (PendingIds.Count != cachedCount)
            {
                Logger.Log(
                    $"Remaining buildIds: ({PendingIds.Count}) {string.Join(", ", PendingIds)}"
                );
                cachedCount = PendingIds.Count;
            }

            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }

    /// <summary>
    /// Called from main build server. Sends web request to offload server and gets a buildId in return
    /// </summary>
    public async Task SendAsync()
    {
        var offload = new OffloadBuild();
        var remoteBuild = new OffloadBuild.Payload { SendBackUrl = SendBackUrl, Packet = Packet };

        var res = await Web.SendAsync(HttpMethod.Post, Url + offload.Path, body: remoteBuild);
        Logger.Log($"{nameof(Offloader)}: {res}");
    }

    private void CreateRemoteBuildTargetConfig(
        OffloadServerPacket packet,
        BuildSettingsAsset build,
        BuildTargetFlag flag
    )
    {
        var buildConfig = new OffloadBuildConfig { AssetName = build.Name, };

        switch (flag)
        {
            case BuildTargetFlag.None:
                break;
            case BuildTargetFlag.Standalone:
                break;
            case BuildTargetFlag.Win:
                break;
            case BuildTargetFlag.Win64:
                break;
            case BuildTargetFlag.OSXUniversal:
                break;
            case BuildTargetFlag.Linux64:
                break;
            case BuildTargetFlag.iOS when BuildConfig?.Deploy?.AppleStore is true:
                buildConfig.Deploy = new RemoteAppleDeploy
                {
                    WorkspaceName = WorkspaceName,
                    Config = XcodeConfig
                };
                break;
            case BuildTargetFlag.Android:
                break;
            case BuildTargetFlag.WebGL:
                break;
            case BuildTargetFlag.WindowsStoreApps:
                break;
            case BuildTargetFlag.tvOS:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
        }

        var buildId = Guid.NewGuid().ToString();
        packet.Builds[buildId] = buildConfig;
        PendingIds.Add(buildId);
    }
}

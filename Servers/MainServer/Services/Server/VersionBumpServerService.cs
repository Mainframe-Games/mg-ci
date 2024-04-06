using MainServer.Configs;
using MainServer.Workspaces;
using Newtonsoft.Json.Linq;
using ServerShared;
using SocketServer;

namespace MainServer.Services.Server;

internal sealed class VersionBumpServerService(
    SocketServer.Server server,
    ServerConfig serverConfig
) : ServerService(server)
{
    private static JObject RunUnity(string projectPath, bool standalone, bool android, bool ios)
    {
        throw new NotImplementedException();

        // var unityVersionBump = new UnityVersionBump(
        //     projectPath,
        //         standalone,
        //         android,
        //         ios);
        //
        // unityVersionBump.Run(
        //     out var outBundle,
        //     out var outStandalone,
        //     out var outAndroid,
        //     out var outIos
        // );
        //
        // return new JObject
        // {
        //     ["Bundle"] = outBundle,
        //     ["Standalone"] = outStandalone,
        //     ["Android"] = outAndroid,
        //     ["Ios"] = outIos
        // };
    }

    public override string Name => "version-bump";

    public override void OnStringMessage(string message)
    {
        throw new NotImplementedException();
    }

    public override void OnDataMessage(byte[] data)
    {
        throw new NotImplementedException();
    }

    public override void OnJsonMessage(JObject payload)
    {
        var projectId = payload["Guid"]?.ToString() ?? throw new NullReferenceException();
        var branch = payload["Branch"]?.ToString() ?? throw new NullReferenceException();
        var projectGuid = new Guid(projectId);
        var workspace = WorkspaceUpdater.PrepareWorkspace(projectGuid, branch, serverConfig);

        var response = new JObject();

        switch (workspace.Engine)
        {
            case GameEngine.Unity:
                var standalone = payload.SelectToken("Standalone", true)?.ToObject<bool>() ?? false;
                var android = payload.SelectToken("Android", true)?.ToObject<bool>() ?? false;
                var ios = payload.SelectToken("Ios", true)?.ToObject<bool>() ?? false;
                response = RunUnity(workspace.ProjectPath, standalone, android, ios);
                break;
            case GameEngine.Godot:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException();
        }

        SendJson(response);
    }
}

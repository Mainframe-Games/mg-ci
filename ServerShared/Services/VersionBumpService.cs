using Newtonsoft.Json.Linq;
using ServerShared;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace OffloadServer;

public class VersionBumpService : WebSocketBehavior
{
    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);

        var payload = JObject.Parse(e.Data) ?? throw new NullReferenceException();
        var projectId = payload["Guid"]?.ToString() ?? throw new NullReferenceException();
        var projectGuid = new Guid(projectId);
        var workspace =
            WorkspaceUpdater.PrepareWorkspace(projectGuid) ?? throw new NullReferenceException();

        var response = new JObject();
        
        switch (workspace.Engine)
        {
            case GameEngine.Unity:
                var standalone =
                    payload.SelectToken("Standalone", true)?.ToObject<bool>() ?? false;
                var android =
                    payload.SelectToken("Android", true)?.ToObject<bool>() ?? false;
                var ios =
                    payload.SelectToken("Ios", true)?.ToObject<bool>() ?? false;
                RunUnity(workspace.ProjectPath, standalone, android, ios);
                break;
            case GameEngine.Godot:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException();
        }

        Send(response.ToString());
    }

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
}

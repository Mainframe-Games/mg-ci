using Newtonsoft.Json.Linq;
using OffloadServer.Utils;
using UnityBuilder;
using WebSocketSharp;

namespace OffloadServer;

internal class VersionBumpService : ServiceBase
{
    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);

        var payload = JObject.Parse(e.Data) ?? throw new NullReferenceException();
        var projectId = payload["Guid"]?.ToString() ?? throw new NullReferenceException();
        var workspace =
            WorkspaceUpdater.PrepareWorkspace(projectId) ?? throw new NullReferenceException();

        var response = workspace.Engine switch
        {
            "Unity" => RunUnity(workspace.ProjectPath, payload),
            "Godot" => throw new NotImplementedException(),
            _ => new JObject()
        };

        Send(response.ToString());
    }

    private static JObject RunUnity(string projectPath, JToken payload)
    {
        var projectSettingsPath = Path.Combine(
            projectPath,
            "ProjectSettings",
            "ProjectSettings.asset"
        );

        var standalone =
            payload.SelectToken("Prebuild.BuildNumberStandalone", true)?.ToObject<bool>() ?? false;
        var android =
            payload.SelectToken("Prebuild.AndroidVersionCode", true)?.ToObject<bool>() ?? false;
        var ios =
            payload.SelectToken("Prebuild.BuildNumberIphone", true)?.ToObject<bool>() ?? false;

        var unityVersionBump = new UnityVersionBump(
                projectSettingsPath,
                standalone,
                android,
                ios);
        
        unityVersionBump.Run(
            out var outBundle,
            out var outStandalone,
            out var outAndroid,
            out var outIos
        );

        return new JObject
        {
            ["Bundle"] = outBundle,
            ["Standalone"] = outStandalone,
            ["Android"] = outAndroid,
            ["Ios"] = outIos
        };
    }
}

using Newtonsoft.Json.Linq;
using Server.Services;
using UnityBuilder;
using WebSocketSharp;

namespace BuildRunner;

public class VersionBumpService : ServiceBase
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

        UnityVersionBump.Run(
            projectSettingsPath,
            standalone,
            android,
            ios,
            out var bundle,
            out var outStandalone,
            out var outAndroid,
            out var outIos
        );

        return new JObject
        {
            ["bundle"] = bundle,
            ["standalone"] = outStandalone,
            ["android"] = outAndroid,
            ["ios"] = outIos
        };
    }
}

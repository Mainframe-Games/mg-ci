using ServerClientShared;
using SharedLib;
using UnityBuilder;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace BuildRunner;

public class BuildRunnerService : ServiceBase
{
    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);

        var payload =
            Json.Deserialise<BuildRunnerPayload>(e.Data) ?? throw new NullReferenceException();

        switch (payload.GameEngine)
        {
            case "Unity":
                // TODO: Get the workspace from the payload
                var unityBuildRunner = new UnityBuildRunner();
                unityBuildRunner.Run();
                break;

            case "Godot":
                throw new NotImplementedException();
        }
    }
}

public abstract class ServiceBase : WebSocketBehavior
{
    protected void Send(NetworkPayload payload)
    {
        var json = Json.Serialise(payload);
        Send(json);
    }
}

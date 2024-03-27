using ServerClientShared;
using SharedLib;
using SharedLib.BuildToDiscord;
using WebSocketSharp;

namespace Server.Services;

public class ReportService : ServiceBase
{
    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);

        Console.WriteLine($"Build start requested: {e.Data}");
        var json = Json.Deserialise<NetworkPayload>(e.Data) ?? throw new NullReferenceException();

        switch (json.Type)
        {
            case MessageType.Connection:
                break;
            case MessageType.Disconnection:
                break;
            case MessageType.Message:
                
                var projectGuid = json.Data?.ToString() ?? throw new NullReferenceException();
                var data = GatherPipelineState(projectGuid);
                
                if (data is not null)
                    Send(new NetworkPayload(MessageType.Message, 0, data));
                else
                    Send(new NetworkPayload(MessageType.Error, 0, $"Pipeline not found: {projectGuid}"));
                
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static PipelineReport? GatherPipelineState(string projectGuid)
    {
        if (!App.PipelinesMap.TryGetValue(projectGuid, out var pipeline))
            return null;

        return pipeline.Report;
    }
}

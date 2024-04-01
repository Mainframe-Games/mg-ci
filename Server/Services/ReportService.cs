using ServerClientShared;
using SharedLib.BuildToDiscord;
using WebSocketSharp;

namespace Server.Services;

public class ReportService : ServiceBase
{
    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);

        // var projectGuid = payload.Data?.ToString() ?? throw new NullReferenceException();
        // var data = GatherPipelineState(projectGuid);
        //
        // if (data is not null)
        //     Send(new NetworkPayload(MessageType.Message, data));
    }

    private static PipelineReport? GatherPipelineState(string projectGuid)
    {
        if (!App.Pipelines.TryGetValue(projectGuid, out var pipeline))
            return null;

        return pipeline.Report;
    }
}

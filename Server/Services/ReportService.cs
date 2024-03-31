using ServerClientShared;
using SharedLib.BuildToDiscord;

namespace Server.Services;

public class ReportService : ServiceBase
{
    protected override void OnMessage(NetworkPayload payload)
    {
        switch (payload.Type)
        {
            case MessageType.Connection:
                break;
            case MessageType.Disconnection:
                break;
            case MessageType.Message:

                var projectGuid = payload.Data?.ToString() ?? throw new NullReferenceException();
                var data = GatherPipelineState(projectGuid);

                if (data is not null)
                    Send(new NetworkPayload(MessageType.Message, 0, data));
                else
                    Send(
                        new NetworkPayload(
                            MessageType.Error,
                            0,
                            $"Pipeline not found: {projectGuid}"
                        )
                    );

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static PipelineReport? GatherPipelineState(string projectGuid)
    {
        if (!App.Pipelines.TryGetValue(projectGuid, out var pipeline))
            return null;

        return pipeline.Report;
    }
}

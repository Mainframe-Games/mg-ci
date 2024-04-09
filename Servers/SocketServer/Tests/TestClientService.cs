using Newtonsoft.Json.Linq;

namespace SocketServer.Tests;

public class TestClientService(Client client) : ClientService(client)
{
    public override string Name => "test";

    public override void OnStringMessage(string message) { }

    public override void OnDataMessage(byte[] data) { }

    public override void OnJsonMessage(JObject payload) { }

    internal override void OnConnected()
    {
        base.OnConnected();

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(home, "ci-cache", "Unity Test", "Builds");

        var projectGuid = Guid.NewGuid();

        FileUploader.UploadDirectory(
            projectGuid,
            new DirectoryInfo(Path.Combine(path, "Windows")),
            this
        );
        FileUploader.UploadDirectory(
            projectGuid,
            new DirectoryInfo(Path.Combine(path, "Linux")),
            this
        );
        FileUploader.UploadDirectory(
            projectGuid,
            new DirectoryInfo(Path.Combine(path, "Mac")),
            this
        );
    }
}

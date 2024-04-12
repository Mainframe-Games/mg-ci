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

        var projectGuid = new Guid("25f1670b-3be3-4f75-aacc-3b5390d355a0");
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(home, "ci-cache", projectGuid.ToString(), "Builds");

        var buildsDir = new DirectoryInfo(path);
        foreach (var directory in buildsDir.GetDirectories())
        {
            if (directory.Name == "Logs")
                continue;

            FileUploader.UploadDirectory(projectGuid, directory, this);
        }
    }
}

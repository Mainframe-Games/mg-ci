using SocketServer;
using SocketServer.Test;

var server = new Server(8080);
server.Start();

var client = new Client("127.0.0.1", 8080);
await UploadFile.UploadDirectory(
    new DirectoryInfo("C:/Users/Brogan/ci-cache/Unity Test/Builds/Windows"),
    client
);

Console.Read();

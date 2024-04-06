using SocketServer;

var server = new Server(8080);
server.Start();

var name = Environment.MachineName;

// var client = new Client("127.0.0.1", 8080);
// var service = client.AddService("UploadService");
// await FileUploader.UploadDirectory(
//     new DirectoryInfo("C:/Users/Brogan/ci-cache/Unity Test/Builds/Windows"),
//     client
// );

Console.Read();

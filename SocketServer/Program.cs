using SocketServer;


// tpc 
var server = new Server(8080);
server.Start();

var client = new Client("127.0.0.1", 8080);

await Task.Delay(2000);

// Send message to the server
await client.Send("Hello, Server!");
await Task.Delay(2000);
await client.Send("Hello, Server! 2");

client.Close();


// web socket

// var server = new WebSocketServer("http://127.0.0.1:8080/");
// server.Start();
//
// var client = new WebSocketClient("ws://127.0.0.1:8080/");
// await client.ConnectAsync();
//
// await UploadFile.Upload(
//     new DirectoryInfo("/Users/broganking/ci-cache/Unity Test/Builds/Windows"),
//     client);

Console.Read();
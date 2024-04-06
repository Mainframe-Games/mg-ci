using SocketServer;
using SocketServer.Tests;

var server = new Server(8080);
server.AddService(new TestServerService(server));
server.Start();

var client = new Client("127.0.0.1", 8080);
var clientService = new TestClientService(client);
client.AddService(clientService);

Console.Read();

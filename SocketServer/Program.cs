using System.Text;
using SocketServer;

StartServer();
await StartClient();
Console.Read();
return;

static void StartServer()
{
    var server = new WebSocketServer("http://127.0.0.1:8080/");
    server.Start();
}

static async Task StartClient()
{
    var client = new WebSocketClient("ws://127.0.0.1:8080/");

    await client.ConnectAsync();

    // for (int i = 0; i < 3; i++)
    // {
    //     var str = new StringBuilder("Start of message");
    //     for (int j = 0; j <= 10_000_000; j++)
    //         str.Append($"Hello, Server! {j}");
    //     await client.SendMessage(str.ToString());
    // }

    var bytes = new byte[Config.KB];
    for (int i = 0; i < bytes.Length; i++)
    {
        var rand = new Random();
        bytes[i] = (byte)rand.Next(0, 255);
    }
    
    await client.SendMessageFragmented(Packet.TestSend());
    
    // for (int i = 0; i < 10; i++)
    // {
    //     // Send message to the server
    //     var b = new byte[1024 * (i + 1)];
    //     await client.SendMessage(b);
    //     await Task.Delay(100);
    // }

    // await client.Close();
    // Console.WriteLine("Connection closed.");
}


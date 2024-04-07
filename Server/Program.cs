using Server;
using Server.Configs;
using Server.Services;
using SharedLib;

// try
// {
//     Console.Title = $"Build Server - {App.ServerVersion}";
//
//     var config = ServerConfig.Load();
//     var mainServer = StartMainServer(config.IP, config.Port);
//
//     // start runners
//     BuildRunnerFactory.Init(config.Runners);
//
//     Console.WriteLine("\nPress Enter key to stop the server...");
//     Console.ReadLine();
//     mainServer.Stop();
// }
// catch (Exception e)
// {
//     Logger.Log(e);
// }

Console.WriteLine("---- End of program ----");
Console.Read();
return;

// static WebSocketServer StartMainServer(string ip, ushort port)
// {
//     var server = new WebSocketServer($"ws://{ip}:{port}");
//     server.AddWebSocketService<BuildService>("/build");
//     server.AddWebSocketService<ReportService>("/report");
//     server.Start();
//
//     Console.WriteLine($"Server started on {server.Address}:{server.Port}");
//
//     if (server.IsListening)
//     {
//         Console.WriteLine("Listening on port {0}, and providing WebSocket services:", server.Port);
//
//         foreach (var path in server.WebSocketServices.Paths)
//             Console.WriteLine("- {0}", path);
//     }
//
//     return server;
// }

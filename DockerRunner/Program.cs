
Console.WriteLine("Program variables:");

var env = Environment.GetEnvironmentVariables();
foreach (var e in env)
    Console.WriteLine(e);

Console.WriteLine("Hello world in Docker!");
Console.WriteLine("---- End of program ----");

using System.CommandLine;
using CLI.Commands;

var rootCommand = new RootCommand("Mainframe CI Tool");

var commands = typeof(ICommand).Assembly
    .GetTypes()
    .Where(x => x.GetInterface(nameof(ICommand)) is not null)
    .Select(x => Activator.CreateInstance(x) as ICommand ?? throw new NullReferenceException())
    .ToArray();

foreach (var command in commands)
    rootCommand.Add(command.BuildCommand());

// Invoke the command
var parseResult =  rootCommand.Parse(args);
return await parseResult.InvokeAsync();


using System.CommandLine;

namespace CLI.Commands;

public class TestCommand : ICommand
{
    public Command BuildCommand()
    {
        var command = new Command("test");
        
        // Add options or subcommands
        var option = new Option<int>("--count", "-c");
        command.Add(option);

        // Set the handler directly
        command.SetAction(async (result, token) =>
        {
            var count = result.GetRequiredValue(option);
            Console.WriteLine($"Test Count is: {count}");
            await Task.CompletedTask;
            return 0;
        });

        return command;
    }
}
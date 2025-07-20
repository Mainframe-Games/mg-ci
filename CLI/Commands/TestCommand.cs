using System.CommandLine;
using Spectre.Console;

namespace CLI.Commands;

public class TestCommand : ICommand
{
    public Command BuildCommand()
    {
        var command = new Command("test");

        // Set the handler directly
        command.SetAction(async (result, token) =>
        {
            AnsiConsole.Write(
                new FigletText("Mainframe CLI Tools")
                    .LeftJustified()
                    .Color(Color.Red));
            await Task.CompletedTask;
            return 0;
        });

        return command;
    }
}
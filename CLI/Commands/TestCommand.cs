using System.CommandLine;
using Spectre.Console;

namespace CLI.Commands;

public class TestCommand : ICommand
{
    public Command BuildCommand()
    {
        var command = new Command("test");

        // Set the handler directly
        command.SetAction(_ =>
        {
            AnsiConsole.Write(
                new FigletText("Mainframe CLI Tools")
                    .LeftJustified()
                    .Color(Color.Cyan1));
            return 0;
        });

        return command;
    }
}
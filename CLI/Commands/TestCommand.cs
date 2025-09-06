using System.CommandLine;
using Spectre.Console;

namespace CLI.Commands;

public class TestCommand : Command
{
    public TestCommand() : base("test", "Test command")
    {
        SetAction(_ =>
        {
            AnsiConsole.Write(
                new FigletText("Mainframe CLI Tools")
                    .LeftJustified()
                    .Color(Color.Cyan1));
            return 0;
        });
    }
}
using System.CommandLine;
using Spectre.Console;

namespace MG_CLI;

public class TestCommand : Command
{
    public TestCommand() : base("test", "Test command")
    {
        SetAction(_ =>
        {
            AnsiConsole.Write(
                new FigletText("Mainframe MG-CLI Tools")
                    .LeftJustified()
                    .Color(Color.Cyan1));
            return 0;
        });
    }
}
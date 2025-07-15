using System.CommandLine;

namespace CLI.Commands;

public interface ICommand
{
    Command BuildCommand();
}
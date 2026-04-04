using System.CommandLine;

namespace MG_CLI;

public class ItchioCommands : Command
{
    public ItchioCommands() : base("itchio", "Manage itchio.io related tasks")
    {
        Add(new ItchioButlerSetup());
        Add(new ItchioDeploy());
    }
}
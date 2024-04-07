using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using SharedLib;

namespace DiscordBot.Commands;

public class BuildCommand : Command
{
    public override string? CommandName => DiscordWrapper.Config.CommandName ?? "start-build";
    public override string? Description => "Starts a build from discord";
    public Embed Embed { get; private set; }

    // public WorkspaceMeta? WorkspaceMeta { get; private set; }

    public override SlashCommandProperties Build()
    {
        return CreateCommand()
            .AddOptions(WorkspaceOptions)
            .AddOptions(BuildOptionString("args", "Arguments send to build server", false))
            .Build();
    }

    public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
    {
        try
        {
            var workspaceName = GetOptionValueString(command, "workspace");
            var args = GetOptionValueString(command, "args");

            // request to build server
            var body = new JObject
            {
                ["workspaceName"] = workspaceName,
                ["args"] = args,
                ["commandId"] = command.Id,
                ["discordAddress"] = DiscordWrapper.Config.ListenServer?.Address
            };

            var res = await Web.SendAsync(
                HttpMethod.Post,
                $"{DiscordWrapper.Config.BuildServerUrl}/build",
                body: body
            );
            // var obj = Json.Deserialise<BuildPipelineResponse>(res.Content);
            // WorkspaceMeta = obj?.WorkspaceMeta;

            var template = new SharedLib.Webhooks.Discord.Embed
            {
                AuthorName = command.User.Username,
                AuthorIconUrl = command.User.GetAvatarUrl(),
                // ThumbnailUrl = obj?.WorkspaceMeta?.ThumbnailUrl,
                Title = "Build Started",
                // Description = obj?.ToString() ?? string.Empty,
                Colour = SharedLib.Webhooks.Discord.Colour.GREEN
            };
            Embed = template.BuildEmbed();
            return new CommandResponse(template.Title, template.Description);
        }
        catch (Exception e)
        {
            Logger.Log(e);
            return new CommandResponse("Build Server request failed", e.Message, true);
        }
    }
}

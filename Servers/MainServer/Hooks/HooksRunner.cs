using System.Text;
using MainServer.Utils;
using SharedLib.ChangeLogBuilders;
using SharedLib.Webhooks;
using Tomlyn.Model;
using Workspace = MainServer.Workspaces.Workspace;

namespace MainServer.Hooks;

internal class HooksRunner
{
    private class HookConfig
    {
        public string? Title { get; set; }
        public string? Url { get; set; }
        public bool? IsErrorChannel { get; set; }

        public bool IsDiscord() => Url?.StartsWith("https://discord.com/") ?? false;

        public bool IsSlack() => Url?.StartsWith("https://hooks.slack.com/") ?? false;
    }

    private readonly Workspace _workspace;
    private readonly TomlTable _projectToml;

    private readonly List<HookConfig> _hooks = [];

    private readonly TimeSpan _totalTime;
    private readonly List<(string targetName, TimeSpan buildTime)> _buildResults;

    private readonly string[] _changeLog;
    private readonly string _projectName;
    private readonly string _fullVersion;

    private readonly string _storeUrl;
    private readonly string _storeThumbnailUrl;

    public HooksRunner(
        Workspace workspace,
        TimeSpan totalTime,
        List<(string targetName, TimeSpan buildTime)> buildResults,
        string[] changeLog,
        string fullVersion
    )
    {
        _workspace = workspace;
        _projectToml = workspace.GetProjectToml();

        _totalTime = totalTime;
        _buildResults = buildResults;
        _changeLog = changeLog;
        _fullVersion = fullVersion;

        _storeUrl =
            _projectToml.GetValue<string>("settings", "store_url")
            ?? throw new NullReferenceException();

        _storeThumbnailUrl =
            _projectToml.GetValue<string>("settings", "store_thumbnail_url")
            ?? throw new NullReferenceException();

        _projectName =
            _projectToml.GetValue<string>("settings", "project_name")
            ?? throw new NullReferenceException();

        if (
            !_projectToml.TryGetValue("hooks", out var hooks)
            || hooks is not TomlTableArray hooksArray
        )
            return;

        foreach (var hook in hooksArray)
        {
            _hooks.Add(
                new HookConfig
                {
                    Title = hook["title"] as string,
                    Url = hook["url"] as string,
                    IsErrorChannel = hook["is_error_channel"] as bool? ?? false
                }
            );
        }
    }

    public void Run()
    {
        if (_hooks.Count == 0)
            return;

        // build changeLog
        var hookMessage = new StringBuilder();
        hookMessage.AppendLine($@"**Targets:** Total Time {_totalTime:hh\:mm\:ss}");
        foreach (var (name, time) in _buildResults)
            hookMessage.AppendLine($@"- **{name}**: {time:hh\:mm\:ss}");
        hookMessage.AppendLine();

        hookMessage.AppendLine("**Change Log:**");
        var discord = new ChangeLogBuilderDiscord();
        discord.BuildLog(_changeLog);
        hookMessage.AppendLine(discord.ToString());

        var title = $"{_projectName} | {_fullVersion}";

        // send hooks
        foreach (var hook in _hooks)
        {
            if (hook.IsErrorChannel is true)
                continue;

            if (hook.IsDiscord())
            {
                var embed = new Discord.Embed
                {
                    Url = _storeUrl,
                    ThumbnailUrl = _storeThumbnailUrl,
                    Title = title,
                    Description = hookMessage.ToString(),
                    Username = hook.Title,
                    Colour = Discord.Colour.GREEN
                };
                Discord.PostMessage(hook.Url!, embed);
            }
            else if (hook.IsSlack())
            {
                var slackMessage = $"*{hook.Title}*\n{title}\n{hookMessage}";
                Slack.PostMessage(hook.Url!, slackMessage);
            }
        }
    }
}

using System.CommandLine;
using System.Text;
using CLI.Utils;
using CliWrap;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Command = System.CommandLine.Command;

namespace CLI.Commands;

public class DiscordHook : Command
{
    private readonly Option<string> _projectPath = new("--projectPath", "-p")
    {
        HelpName = "Path to the Godot project"
    };

    private readonly Option<string> _hookUrl = new("--hookUrl", "-h")
    {
        HelpName = "Url to discord channel"
    };

    private readonly Option<string> _steamUrl = new("--steamUrl", "-s")
    {
        HelpName = "Url to steam page"
    };

    private readonly Option<string> _logoUrl = new("--logoUrl", "-l")
    {
        HelpName = "Url to steam capsule"
    };

    private readonly Option<bool> _noChangeLog = new("--noChangeLog")
    {
        HelpName = "Skips change log step in description",
    };
    
    public DiscordHook() : base("discord-hook", "Send a Discord webhook with the latest build info")
    {
        Add(_projectPath);
        Add(_hookUrl);
        Add(_steamUrl);
        Add(_logoUrl);
        Add(_noChangeLog);
        SetAction(Run);
    }

    private async Task<int> Run(ParseResult result, CancellationToken token)
    {
        string projectPath = result.GetRequiredValue(_projectPath);
        string hookUrl = result.GetRequiredValue(_hookUrl);
        string steamUrl = result.GetRequiredValue(_steamUrl);
        string logoUrl = result.GetRequiredValue(_logoUrl);
        bool noChangeLog = result.GetValue(_noChangeLog);
            
        // get all tags
        var tags = new List<string>();
        var res = await Cli.Wrap("git")
            .WithArguments("tag --sort=-creatordate")
            .WithWorkingDirectory(projectPath)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(tags.Add))
            .ExecuteAsync(token);

        if (res.ExitCode != 0)
            return res.ExitCode;
        
        // get all commits 
        var commits = new List<string>();
        if (!noChangeLog)
        {
            var prevTag = tags[1];
            res = await Cli.Wrap("git")
                .WithArguments($"log {prevTag}..HEAD --oneline")
                .WithWorkingDirectory(projectPath)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(commits.Add))
                .WithStandardErrorPipe(CliWrapExtensions.StdErrorPipe)
                .ExecuteAsync(token);
            
            if (res.ExitCode != 0)
                return res.ExitCode;
            
            for (int i = commits.Count - 1; i >= 0; i--)
            {
                var split = commits[i].Split(' ');
                commits[i] = string.Join(" ", split[1..]); // remove SHA
            }
        }
        
        // send POST request
        var version = GodotVersioning.GetVersion(projectPath);
        var description = noChangeLog ? "" : $"**Change Log:**\n{ParseCommits(commits)}";
        var json = new JObject
        {
            ["embeds"] = new JArray(
                new JObject
                {
                    ["title"] = $"New Build Available! | {version}",
                    ["url"] = steamUrl,
                    ["description"] = description,
                    ["color"] = 65280,
                    ["thumbnail"] = new JObject
                    {
                        ["url"] = logoUrl
                    }
                })
        };
        
        using var client = new HttpClient();
        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(hookUrl, content, token);

        if (!response.IsSuccessStatusCode)
        {
            Log.WriteLine($"Discord hook failed: {response.StatusCode}, reason: {response.ReasonPhrase}", Color.Red);
            return (int)response.StatusCode;
        }
        
        return 0;
    }

    private static string ParseCommits(List<string> commits)
    {
        // var fixes = new StringBuilder();
        // var additions = new StringBuilder();
        // var removals = new StringBuilder();
        
        var outStr = new StringBuilder();

        foreach (var commit in commits)
        {
            if (commit.StartsWith('_'))
                continue;
            
            // var prefix = commit.ToLower().Trim().Split(' ')[0];
            //
            // switch (prefix)
            // {
            //     case "fix" or "fixed" or "change" or "changed":
            //         fixes.AppendLine($"- {commit}");
            //         break;
            //     
            //     case "add" or "added":
            //         additions.AppendLine($"- {commit}");
            //         break;
            //
            //     case "remove" or "removed":
            //         removals.AppendLine($"- {commit}");
            //         break;
            //     
            //     default:
            //         break;
            // }
            
            outStr.AppendLine($"- {commit}");
            
        }
        
        // if (fixes.Length > 0)
        // {
        //     outStr.AppendLine("**Fixes:**");
        //     outStr.AppendLine(fixes.ToString());
        // }
        // if (additions.Length > 0)
        // {
        //     outStr.AppendLine("**Additions:**");
        //     outStr.AppendLine(additions.ToString());
        // }
        // if (removals.Length > 0)
        // {
        //     outStr.AppendLine("**Removals:**");
        //     outStr.AppendLine(removals.ToString());
        // }
        
        return outStr.ToString();
    }
}
using System.CommandLine;
using System.Text;
using CLI.Utils;
using CliWrap;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Command = System.CommandLine.Command;

namespace CLI.Commands;

public class DiscordHook : ICommand
{
    public Command BuildCommand()
    {
        var command = new Command("discord-hook");
        
        var projectPath = new Option<string>("--projectPath", "-p")
        {
            HelpName = "Path to the Godot project"
        };
        command.Add(projectPath);
        
        var hookUrl = new Option<string>("--hookUrl", "-h")
        {
            HelpName = "Url to discord channel"
        };
        command.Add(hookUrl);
        
        var steamUrl = new Option<string>("--steamUrl", "-s")
        {
            HelpName = "Url to steam page"
        };
        command.Add(steamUrl);
        
        var logoUrl = new Option<string>("--logoUrl", "-l")
        {
            HelpName = "Url to steam capsule"
        };
        command.Add(logoUrl);
        
        var noChangeLog = new Option<bool>("--noChangeLog")
        {
            HelpName = "Url to steam capsule",
        };
        command.Add(logoUrl);
        
        // Set the handler directly
        command.SetAction(async (result, token)
            =>
        {
            try
            {
                return await Run(
                    result.GetRequiredValue(projectPath), 
                    result.GetRequiredValue(hookUrl), 
                    result.GetRequiredValue(steamUrl), 
                    result.GetRequiredValue(logoUrl),
                    result.GetValue(noChangeLog)
                    );
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return -1;
            }
        });
        
        return command;
    }

    private static async Task<int> Run(string projectPath, string hookUrl, string steamUrl, string logoUrl, bool noChangeLog)
    {
        // get all tags
        var tags = new List<string>();
        var res = await Cli.Wrap("git")
            .WithArguments("tag --sort=-creatordate")
            .WithWorkingDirectory(projectPath)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(tags.Add))
            .ExecuteAsync();

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
                .ExecuteAsync();
            
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
        var response = await client.PostAsync(hookUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            Log.WriteLine($"Discord hook failed: {response.StatusCode}, reason: {response.ReasonPhrase}", Color.Red);
            return (int)response.StatusCode;
        }
        
        return 0;
    }

    private static string ParseCommits(List<string> commits)
    {
        var fixes = new StringBuilder();
        var additions = new StringBuilder();
        var removals = new StringBuilder();

        foreach (var commit in commits)
        {
            var prefix = commit.ToLower().Trim().Split(' ')[0];

            switch (prefix)
            {
                case "fix" or "fixed" or "change" or "changed":
                    fixes.AppendLine($"- {commit}");
                    break;
                
                case "add" or "added":
                    additions.AppendLine($"- {commit}");
                    break;

                case "remove" or "removed":
                    removals.AppendLine($"- {commit}");
                    break;
                
                default:
                    // do nothing, ignore
                    break;
            }
        }
        
        var outStr = new StringBuilder();
        if (fixes.Length > 0)
        {
            outStr.AppendLine("**Fixes:**");
            outStr.AppendLine(fixes.ToString());
        }
        if (additions.Length > 0)
        {
            outStr.AppendLine("**Additions:**");
            outStr.AppendLine(additions.ToString());
        }
        if (removals.Length > 0)
        {
            outStr.AppendLine("**Removals:**");
            outStr.AppendLine(removals.ToString());
        }
        return outStr.ToString();
    }
}
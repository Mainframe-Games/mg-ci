using System.IO.Compression;
using BuildRunner.Utils;
using Newtonsoft.Json.Linq;
using Server.Services;
using SharedLib;
using UnityBuilder;
using WebSocketSharp;

namespace BuildRunner;

public class BuildRunnerService : ServiceBase
{
    private static readonly Dictionary<string, bool> _platform =
        new()
        {
            ["windows"] = Arg.IsFlag("-windows"),
            ["macos"] = Arg.IsFlag("-macos"),
            ["linux"] = Arg.IsFlag("-linux"),
        };

    private static readonly Queue<KeyValuePair<string, JObject>> _buildQueue = new();
    private static Task? _buildRunnerTask;

    protected override void OnOpen()
    {
        base.OnOpen();
        Console.WriteLine($"Client connected [{Context.RequestUri.AbsolutePath}]: {ID}");

        var keys = _platform.Where(x => x.Value).Select(x => x.Key).ToArray();
        var platforms = string.Join(" ", keys).Trim();
        if (platforms.Length > 0)
            Send(platforms);
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);

        var payload = JObject.Parse(e.Data) ?? throw new NullReferenceException();
        var targetName = payload["TargetName"]?.ToString() ?? throw new NullReferenceException();

        var item = new KeyValuePair<string, JObject>(targetName, payload);
        _buildQueue.Enqueue(item);
        Send(new JObject { ["TargetName"] = item.Key, ["Status"] = "Queued", }.ToString());

        BuildQueued();
    }

    private async void BuildQueued()
    {
        // prevent multiple build runners
        if (_buildRunnerTask is not null)
            return;

        while (_buildQueue.Count > 0)
        {
            var (key, payload) = _buildQueue.Dequeue();

            var projectId =
                payload.SelectToken("Project.Guid", true)?.ToString()
                ?? throw new NullReferenceException();

            var workspace =
                WorkspaceUpdater.PrepareWorkspace(projectId) ?? throw new NullReferenceException();

            Console.WriteLine($"Queuing build: {key}");
            Send(new JObject { ["TargetName"] = key, ["Status"] = "Building" }.ToString());

            _buildRunnerTask = workspace.Engine switch
            {
                "Unity" => Task.Run(() => RunUnity(workspace.ProjectPath, payload)),
                "Godot" => throw new NotImplementedException(),
                _ => throw new ArgumentException()
            };

            // await build to be done
            await _buildRunnerTask;
            _buildRunnerTask = null;
        }

        Console.WriteLine("Build queue empty");
    }

    private void RunUnity(string projectPath, JObject payload)
    {
        var targetName = payload["TargetName"]?.ToString() ?? throw new NullReferenceException();
        var buildTarget = payload["BuildTarget"]?.ToString() ?? throw new NullReferenceException();
        var target =
            payload
                .SelectToken("Project.BuildTargets", true)
                ?.FirstOrDefault(x => x["Name"]?.ToString() == targetName)
            ?? throw new NullReferenceException();

        // run build
        var unityRunner = new UnityBuild2(projectPath, target, buildTarget);
        unityRunner.Run();

        // zip build content and send back
        ZipFileForServer(unityRunner.BuildPath, targetName);
    }

    private void ZipFileForServer(string buildPath, string targetName)
    {
        var zipPath = $"{buildPath}.zip";

        if (File.Exists(zipPath))
            File.Delete(zipPath);

        ZipFile.CreateFromDirectory(buildPath, zipPath);
        ZipMeta(zipPath, targetName);
        // SendZipFile(zipPath, targetName);
    }

    private void ZipMeta(string zipFilePath, string targetName)
    {
        using var archive = ZipFile.OpenRead(zipFilePath);
        Console.WriteLine($"Entries in the zip file '{zipFilePath}':");

        foreach (var entry in archive.Entries)
        {
            Console.WriteLine($"- Name: {entry.FullName}");
            Console.WriteLine($"  Size: {entry.Length} bytes");

            // TODO: upload files from here
            SendZipFile(entry, targetName);
        }
    }

    private async void SendZipFile(ZipArchiveEntry zipEntry, string targetName)
    {
        const int fragmentSize = 1024 * 10; // 10 KB

        var stream = zipEntry.Open();
        var data = new byte[stream.Length];
        var readAsync = await stream.ReadAsync(data);

        // Calculate the number of fragments
        var totalFragments = (int)Math.Ceiling((double)data.Length / fragmentSize);

        Console.WriteLine($"Sending file [{data.Length} bytes]: {zipEntry.FullName}");

        // Send each fragment
        for (int i = 0; i < totalFragments; i++)
        {
            // Calculate the start and end index for the current fragment
            var startIndex = i * fragmentSize;
            var endIndex = Math.Min((i + 1) * fragmentSize, data.Length);

            // Extract the current fragment from the original data
            var fragment = new byte[endIndex - startIndex];
            Array.Copy(data, startIndex, fragment, 0, fragment.Length);

            // Write the target name, total length, and fragment to a memory stream
            using var ms = new MemoryStream();
            await using var writer = new BinaryWriter(ms);
            writer.Write(targetName);
            writer.Write(data.Length);
            writer.Write(fragment.Length);
            writer.Write(fragment);

            // Send the fragment
            Send(ms.ToArray());

            // need to delay some time to give server time to process
            await Task.Delay(1);
        }

        Console.WriteLine($"Sending file complete: {zipEntry.FullName}");
    }

    private async void SendZipFile(string filePath, string targetName)
    {
        const int fragmentSize = 1024 * 10; // 10 KB

        var data = await File.ReadAllBytesAsync(filePath);

        // Calculate the number of fragments
        var totalFragments = (int)Math.Ceiling((double)data.Length / fragmentSize);

        Console.WriteLine($"Sending file [{data.Length} bytes]: {filePath}");

        // Send each fragment
        for (int i = 0; i < totalFragments; i++)
        {
            // Calculate the start and end index for the current fragment
            var startIndex = i * fragmentSize;
            var endIndex = Math.Min((i + 1) * fragmentSize, data.Length);

            // Extract the current fragment from the original data
            var fragment = new byte[endIndex - startIndex];
            Array.Copy(data, startIndex, fragment, 0, fragment.Length);

            // Write the target name, total length, and fragment to a memory stream
            using var ms = new MemoryStream();
            await using var writer = new BinaryWriter(ms);
            writer.Write(targetName);
            writer.Write(data.Length);
            writer.Write(fragment.Length);
            writer.Write(fragment);

            // Send the fragment
            Send(ms.ToArray());

            // need to delay some time to give server time to process
            await Task.Delay(1);
        }

        Console.WriteLine($"Sending file complete: {filePath}");
        Send(new JObject { ["TargetName"] = targetName, ["Status"] = "Complete" }.ToString());
    }
}

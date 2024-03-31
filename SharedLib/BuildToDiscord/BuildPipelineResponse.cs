using System.Reflection;
using System.Text;

namespace SharedLib.BuildToDiscord;

public class BuildPipelineResponse
{
    public string? ServerVersion { get; set; }
    public string? PipelineId { get; set; }
    public string? Workspace { get; set; }
    public WorkspaceMeta? WorkspaceMeta { get; set; }
    public string? Targets { get; set; }
    public string? Branch { get; set; }
    public int? ChangesetId { get; set; }
    public string? ChangesetGuid { get; set; }
    public string? UnityVersion { get; set; }
    public int? ChangesetCount { get; set; }

    public override string ToString()
    {
        var str = new StringBuilder();

        var properties = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

        foreach (var propertyInfo in properties)
        {
            // skip meta data
            if (propertyInfo.Name is nameof(WorkspaceMeta))
                continue;

            var value = propertyInfo.GetValue(this);

            if (value is null)
                continue;

            str.AppendLine($"**{propertyInfo.Name}**: {value}");
        }

        return str.ToString();
    }
}

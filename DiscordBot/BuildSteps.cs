using System.Text;
using Discord;

namespace DiscordBot;

public enum BuildTaskState
{
	Pending,
	Succeed,
	Failed
}

public class BuildSteps
{
	private static readonly Dictionary<BuildTaskState, string> Emotes = new()
	{
		[BuildTaskState.Pending] = "⌛",
		[BuildTaskState.Succeed] = "✅",
		[BuildTaskState.Failed] = "❌",
	};

	private string Name { get; }
	public BuildTaskState State { get; set; }
	public readonly List<BuildSteps> SubTasks;

	public BuildSteps(string name, params BuildSteps[] subTasks)
	{
		Name = name;
		SubTasks = subTasks.ToList();
	}

	public override string ToString()
	{
		return $"{Emotes[State]} **{Name}** *{State}*";
	}
	
	public EmbedFieldBuilder[] GetSubTaskEmbedFields()
	{
		return SubTasks.Select(step => new EmbedFieldBuilder
		{
			Name = Name,
			Value = $"{Emotes[step.State]} {step.State}"
		}).ToArray();
	}

	public static string BuildEmbedFields(List<BuildSteps> steps)
	{
		var str = new StringBuilder();

		str.AppendLine("**Pipeline Steps**");
		
		foreach (var step in steps)
			str.AppendLine(step.ToString());

		return str.ToString();
	}
}
using System.Text;

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
		SubTasks = subTasks?.ToList() ?? new List<BuildSteps>();
	}

	public override string ToString()
	{
		var str = new StringBuilder();
		str.AppendLine($"{Emotes[State]} {Name}");

		foreach (var step in SubTasks)
			str.AppendLine($"- {step}");

		return str.ToString();
	}

	public static string BuildString(List<BuildSteps> steps)
	{
		var str = new StringBuilder();

		foreach (var step in steps)
			str.AppendLine(step.ToString());

		return str.ToString();
	}
}
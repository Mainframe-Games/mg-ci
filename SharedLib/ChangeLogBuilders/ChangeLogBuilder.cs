﻿using System.Collections.Generic;
using System.Text;

namespace SharedLib.ChangeLogBuilders;

public abstract class ChangelogBuilder
{
	protected abstract Markup List { get; }
	protected abstract Markup ListItem { get; }
	protected abstract Markup Bold { get; }

	private readonly StringBuilder _stringBuilder = new();

	private static bool IgnoreCommit(string line)
	{
		return string.IsNullOrEmpty(line) || line.StartsWith("_");
	}

	private static bool IsKeyword(string firstWord)
	{
		return firstWord.ToLower() switch
		{
			"fixed" => true,
			"added" => true,
			"removed" => true,
			"improved" => true,
			"updated" => true,
			"changed" => true,
			"modified" => true,
			_ => false
		};
	}

	/// <summary>
	/// Returns true if logs are printed
	/// </summary>
	/// <param name="commits"></param>
	/// <returns></returns>
	public void BuildLog(IEnumerable<string> commits)
	{
		if (!string.IsNullOrEmpty(List.Start))
			_stringBuilder.AppendLine(List.Start);

		foreach (var line in commits)
		{
			if (IgnoreCommit(line))
				continue;

			var words = line.Trim().Split(' ');

			if (words.Length == 0 || string.IsNullOrEmpty(words[0]))
				continue;

			var isKeyword = IsKeyword(words[0]);

			for (int i = 0; i < words.Length; i++)
			{
				if (string.IsNullOrEmpty(words[i]) || string.IsNullOrWhiteSpace(words[i]))
					continue;

				if (i == 0)
					_stringBuilder.Append(isKeyword
						? $"{ListItem.Start} {Bold.Start}{words[i]}{Bold.End}"
						: $"{ListItem.Start} {words[i]}");
				else
					_stringBuilder.Append($" {words[i]}");
			}

			_stringBuilder.AppendLine(ListItem.End);
		}

		_stringBuilder.AppendLine(List.End);
	}

	public override string ToString()
	{
		return _stringBuilder.ToString();
	}
}
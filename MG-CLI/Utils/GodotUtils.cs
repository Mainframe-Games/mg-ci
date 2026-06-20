namespace MG_CLI;

/// <summary>
/// Provides utility functions for working with Godot project files.
/// </summary>
public static class GodotUtils
{
	/// <summary>
	/// Retrieves the first project settings file with the extension ".godot"
	/// located within the specified directory or its subdirectories.
	/// </summary>
	/// <param name="fullPath">The full path to the directory where the search for the project settings file begins.</param>
	/// <returns>A <see cref="FileInfo"/> object representing the project settings file found.</returns>
	public static FileInfo GetProjectSettingsFile(string fullPath)
	{
		var dirInfo = new DirectoryInfo(fullPath);
		var file = dirInfo
			.GetFiles("*.godot", SearchOption.AllDirectories)
			.First();
		return file;
	}
}
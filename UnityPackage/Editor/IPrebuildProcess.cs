namespace BuildSystem
{
	/// <summary>
	/// Used to set specific target prebuild methods
	/// </summary>
	public interface IPrebuildProcess
	{
		void OnPrebuildProcess(BuildSettings buildSettings);
	}
}
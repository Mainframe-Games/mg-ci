namespace Deployment.Configs;

public struct ItchioConfig
{
	public string Location { get; set; }
	public string Username { get; set; }
	public string Game { get; set; }

	public bool IsValid()
	{
		return !string.IsNullOrEmpty(Location);
	}
}
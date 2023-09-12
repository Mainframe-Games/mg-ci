namespace SharedLib;

public class WorkspaceMeta : ICloneable
{
	public string? Url { get; set; }
	public string? ThumbnailUrl { get; set; }
	public string? ProjectName { get; set; }
	public object Clone()
	{
		return new WorkspaceMeta
		{
			Url = Url,
			ThumbnailUrl = Url,
			ProjectName = Url,
		};
	}
}
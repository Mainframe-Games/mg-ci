namespace SharedLib;

public class WorkspaceMeta : ICloneable
{
	public string? Url { get; set; }
	public string? ThumbnailUrl { get; set; }
	public string? ProjectName { get; set; }
	
	/// <summary>
	/// Changeset ID from last successful build (plastic)
	/// </summary>
	public int? LastSuccessfulBuild { get; set; }
	
	/// <summary>
	/// Last sha from successful build (git)
	/// </summary>
	public string? LastSuccessfulSha { get; set; }
	
	public object Clone()
	{
		return new WorkspaceMeta
		{
			Url = Url,
			ThumbnailUrl = ThumbnailUrl,
			ProjectName = ProjectName,
		};
	}
}
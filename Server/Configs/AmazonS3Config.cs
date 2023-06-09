namespace Server.Configs;

public class AmazonS3Config
{
	public string? BucketName { get; set; }
	public string? Url { get; set; }
	public string? AccessKey { get; set; }
	public string? SecretKey { get; set; }
}
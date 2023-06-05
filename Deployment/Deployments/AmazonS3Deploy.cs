using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using SharedLib;

namespace Deployment.Deployments;

public class AmazonS3Deploy
{
	private readonly string? _accessKey;
	private readonly string? _secretKey;

	public AmazonS3Deploy(string? accessKey, string? secretKey)
	{
		_accessKey = accessKey;
		_secretKey = secretKey;
	}

	public async Task DeployAsync(string? rootDirPath, string? bucketName)
	{
		var credentials = new BasicAWSCredentials(_accessKey, _secretKey);
		var s3Client = new AmazonS3Client(credentials, RegionEndpoint.APSoutheast2);
		var fileTransferUtility = new TransferUtility(s3Client);


		var dir = new DirectoryInfo(rootDirPath);
		var files = dir.GetFiles();
		var subDirs = dir.GetDirectories();

		foreach (var file in files)
		{
			var path = file.FullName;
			var key = file.Name;
			if (key == ".DS_Store")
				continue;
			
			Logger.Log($"[S3] Uploading File: {file.Name}");
			await fileTransferUtility.UploadAsync(path, bucketName, key);
		}

		foreach (var subDir in subDirs)
		{
			Logger.Log($"[S3] Uploading Dir: {subDir.Name}");
			await fileTransferUtility.UploadDirectoryAsync(subDir.FullName, $"{bucketName}/{subDir.Name}");

		}
		
		Logger.Log($"AmazonS3 bucket: {bucketName} COMPLETED");
	}
}
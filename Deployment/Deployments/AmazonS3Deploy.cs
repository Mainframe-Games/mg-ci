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
		await fileTransferUtility.UploadDirectoryAsync(rootDirPath, bucketName);
		Logger.Log($"AmazonS3 bucket: {bucketName} COMPLETED");
	}
}
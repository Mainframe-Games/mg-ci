using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace Deployment.Deployments;

public class AmazonS3Deploy
{
    private readonly string? _bucketName;
    private readonly TransferUtility _fileTransferUtility;

    // private readonly ProgressBar _progressBar = new();

    public AmazonS3Deploy(string? accessKey, string? secretKey, string? bucketName)
    {
        _bucketName = bucketName;

        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var s3Client = new AmazonS3Client(credentials, RegionEndpoint.APSoutheast2);
        _fileTransferUtility = new TransferUtility(s3Client);
    }

    public async Task DeployAsync(string? rootDirPath)
    {
        var request = new TransferUtilityUploadDirectoryRequest
        {
            BucketName = _bucketName,
            Directory = rootDirPath,
            SearchOption = SearchOption.AllDirectories,
        };

        Console.WriteLine($"AmazonS3 bucket: {_bucketName} STARTED");

        request.UploadDirectoryProgressEvent += RequestOnUploadDirectoryProgressEvent;
        await _fileTransferUtility.UploadDirectoryAsync(request);

        // _progressBar.Dispose();
        _fileTransferUtility.Dispose();

        Console.WriteLine($"AmazonS3 bucket: {_bucketName} COMPLETED");
    }

    private void RequestOnUploadDirectoryProgressEvent(
        object? sender,
        UploadDirectoryProgressArgs e
    )
    {
        var fileInfp = new FileInfo(e.CurrentFile);
        // _progressBar.SetContext($"AmazonS3 uploading... {fileInfp.Name}");
        // _progressBar.Report(e.TransferredBytes / (double)e.TotalBytes);
    }
}

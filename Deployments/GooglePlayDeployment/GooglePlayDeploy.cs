using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;

namespace GooglePlayDeployment;

/// <summary>
/// src: https://stackoverflow.com/questions/66007292/androidpublisherservice-play-developer-api-client-upload-aab-failes-due-to-b
/// </summary>
public class GooglePlayDeploy(
    string packageName,
    string aabfile,
    string credfile,
    string serviceUsername,
    string buildVersionTitle,
    string releaseNotes
)
{
    public async Task Deploy()
    {
        Console.WriteLine(
            $"Using credentials {credfile} with package {packageName} for aab file {aabfile}"
        );

        var keyDataStream = File.OpenRead(credfile);
        var googleCredentials = GoogleCredential
            .FromStream(keyDataStream)
            .CreateWithUser(serviceUsername)
            .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

        var credentials = (ServiceAccountCredential)googleCredentials.UnderlyingCredential;
        var oauthToken = await credentials.GetAccessTokenForRequestAsync(
            AndroidPublisherService.Scope.Androidpublisher
        );
        var service = new AndroidPublisherService();

        // --- Insert Track

        var edit = service.Edits.Insert(new AppEdit { ExpiryTimeSeconds = "3600" }, packageName);
        edit.Credential = credentials;
        var activeEditSession = await edit.ExecuteAsync();
        Console.WriteLine(
            $"[{nameof(GooglePlayDeploy)}] Edits started with id {activeEditSession.Id}"
        );

        // --- List Tracks

        var tracksList = service.Edits.Tracks.List(packageName, activeEditSession.Id);
        tracksList.Credential = credentials;
        var tracksResponse = await tracksList.ExecuteAsync();
        foreach (var track in tracksResponse.Tracks)
        {
            Console.WriteLine($"[{nameof(GooglePlayDeploy)}] Track: {track.TrackValue}");
            Console.WriteLine($"[{nameof(GooglePlayDeploy)}] Releases: ");
            foreach (var rel in track.Releases)
                Console.WriteLine(
                    $"\t{rel.Name} version: {rel.VersionCodes?.FirstOrDefault()} - Status: {rel.Status}"
                );
        }

        // --- Upload aab file

        Console.WriteLine($"[{nameof(GooglePlayDeploy)}] Uploading bundle... {aabfile}");

        await using var fileStream = File.OpenRead(aabfile);
        var upload = service.Edits.Bundles.Upload(
            packageName,
            activeEditSession.Id,
            fileStream,
            "application/octet-stream"
        );
        upload.OauthToken = oauthToken;
        var uploadProgress = await upload.UploadAsync();

        if (uploadProgress is not { Exception: null })
            throw uploadProgress?.Exception
                ?? new Exception($"[{nameof(GooglePlayDeploy)}] Failed to upload");

        Console.WriteLine($"[{nameof(GooglePlayDeploy)}] Upload {uploadProgress.Status}");

        // --- Update track

        // releaseNotes max is 500 (set by google)
        if (releaseNotes.Length > 500)
            releaseNotes = releaseNotes[..500];

        var tracksUpdate = service.Edits.Tracks.Update(
            new Track
            {
                Releases = new List<TrackRelease>(
                    new[]
                    {
                        new TrackRelease
                        {
                            Name = buildVersionTitle,
                            Status = "draft",
                            InAppUpdatePriority = 5,
                            // CountryTargeting = new CountryTargeting { IncludeRestOfWorld = true },
                            ReleaseNotes = new List<LocalizedText>(
                                new[]
                                {
                                    new LocalizedText { Language = "en-US", Text = releaseNotes }
                                }
                            ),
                            VersionCodes = new List<long?>(
                                new[] { (long?)upload?.ResponseBody?.VersionCode }
                            )
                        }
                    }
                )
            },
            packageName,
            activeEditSession.Id,
            "internal"
        );

        Console.WriteLine($"[{nameof(GooglePlayDeploy)}] Uploading track... {tracksUpdate.Track}");

        tracksUpdate.Credential = credentials;
        var trackResult = await tracksUpdate.ExecuteAsync();
        Console.WriteLine($"Track {trackResult?.TrackValue}");

        Console.WriteLine(
            $"[{nameof(GooglePlayDeploy)}] Committing edit... {packageName}, {activeEditSession.Id}"
        );

        var commitResult = service.Edits.Commit(packageName, activeEditSession.Id);
        commitResult.Credential = credentials;
        await commitResult.ExecuteAsync();

        Console.WriteLine($"[{nameof(GooglePlayDeploy)}] {commitResult.EditId} has been committed");

        try
        {
            // update to completed
            // await UpdateTrackToCompleted(service, packageName, activeEditSession, credentials);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static async Task UpdateTrackToCompleted(
        AndroidPublisherService service,
        string packageName,
        AppEdit activeEditSession,
        IHttpExecuteInterceptor credentials
    )
    {
        var tracksUpdate = service.Edits.Tracks.Update(
            new Track
            {
                Releases = new List<TrackRelease>(
                    new[] { new TrackRelease { Status = "completed" } }
                )
            },
            packageName,
            activeEditSession.Id,
            "internal"
        );

        Console.WriteLine(
            $"[{nameof(GooglePlayDeploy)}] Uploading track to completed... {tracksUpdate.Track}"
        );
        tracksUpdate.Credential = credentials;
        var trackResult = await tracksUpdate.ExecuteAsync();

        Console.WriteLine($"Track {trackResult?.TrackValue}");
    }
}

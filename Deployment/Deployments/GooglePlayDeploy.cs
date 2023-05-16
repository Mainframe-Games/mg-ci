using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using SharedLib;

namespace Deployment.Deployments;

public static class GooglePlayDeploy
{
	/// <summary>
	/// src: https://stackoverflow.com/questions/66007292/androidpublisherservice-play-developer-api-client-upload-aab-failes-due-to-b
	/// </summary>
	/// <param name="packageName"></param>
	/// <param name="aabfile"></param>
	/// <param name="credfile"></param>
	/// <param name="serviceUsername"></param>
	/// <param name="releaseNotes"></param>
	public static async Task Deploy(
		string packageName,
		string aabfile,
		string credfile,
		string serviceUsername,
		string buildVersionTitle,
		string releaseNotes)
	{
		Logger.Log($"Using credentials {credfile} with package {packageName} for aab file {aabfile}");

		var keyDataStream = File.OpenRead(credfile);
		var googleCredentials = GoogleCredential.FromStream(keyDataStream)
			.CreateWithUser(serviceUsername)
			.CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

		var credentials = (ServiceAccountCredential)googleCredentials.UnderlyingCredential;
		var oauthToken = await credentials.GetAccessTokenForRequestAsync(AndroidPublisherService.Scope.Androidpublisher);
		var service = new AndroidPublisherService();

		var edit = service.Edits.Insert(new AppEdit { ExpiryTimeSeconds = "3600" }, packageName);
		edit.Credential = credentials;
		var activeEditSession = await edit.ExecuteAsync();
		Logger.Log($"[{nameof(GooglePlayDeploy)}] Edits started with id {activeEditSession.Id}");

		var tracksList = service.Edits.Tracks.List(packageName, activeEditSession.Id);
		tracksList.Credential = credentials;
		var tracksResponse = await tracksList.ExecuteAsync();
		foreach (var track in tracksResponse.Tracks)
		{
			Logger.Log($"[{nameof(GooglePlayDeploy)}] Track: {track.TrackValue}");
			Logger.Log($"[{nameof(GooglePlayDeploy)}] Releases: ");
			foreach (var rel in track.Releases)
				Logger.Log($"\t{rel.Name} version: {rel.VersionCodes?.FirstOrDefault()} - Status: {rel.Status}");
		}

		await using var fileStream = File.OpenRead(aabfile);
		
		Logger.Log($"[{nameof(GooglePlayDeploy)}] Uploading bundle... {aabfile}");

		var upload = service.Edits.Bundles.Upload(packageName, activeEditSession.Id, fileStream, "application/octet-stream");
		upload.OauthToken = oauthToken;
		var uploadProgress = await upload.UploadAsync();

		if (uploadProgress is not { Exception: null })
			throw uploadProgress?.Exception ?? new Exception($"[{nameof(GooglePlayDeploy)}] Failed to upload");

		Logger.Log($"[{nameof(GooglePlayDeploy)}] Upload {uploadProgress.Status}");

		// releaseNotes max is 500 (set by google)
		if (releaseNotes.Length > 500)
			releaseNotes = releaseNotes[..500];

		var tracksUpdate = service.Edits.Tracks.Update(new Track
		{
			Releases = new List<TrackRelease>(new[]
			{
				new TrackRelease
				{
					Name = buildVersionTitle,
					Status = "completed", // "draft"
					InAppUpdatePriority = 5,
					// CountryTargeting = new CountryTargeting { IncludeRestOfWorld = true },
					ReleaseNotes = new List<LocalizedText>(new[] { new LocalizedText { Language = "en-US", Text = releaseNotes } }),
					VersionCodes = new List<long?>(new[] { (long?)upload?.ResponseBody?.VersionCode })
				}
			})
		}, packageName, activeEditSession.Id, "internal");

		Logger.Log($"[{nameof(GooglePlayDeploy)}] Uploading track... {tracksUpdate.Track}");

		tracksUpdate.Credential = credentials;
		var trackResult = await tracksUpdate.ExecuteAsync();
		Logger.Log($"Track {trackResult?.TrackValue}");

		Logger.Log($"[{nameof(GooglePlayDeploy)}] Committing edit... {packageName}, {activeEditSession.Id}");

		var commitResult = service.Edits.Commit(packageName, activeEditSession.Id);
		commitResult.Credential = credentials;
		await commitResult.ExecuteAsync();

		Logger.Log($"[{nameof(GooglePlayDeploy)}] {commitResult.EditId} has been committed");
	}
}
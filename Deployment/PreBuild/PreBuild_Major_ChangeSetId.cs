using Deployment.Misc;
using SharedLib;

namespace Deployment.PreBuild;

public class PreBuild_Major_ChangeSetId : PreBuildBase
{
	/// <summary>
	/// Bumps the change set ID and builds the change log
	/// </summary>
	/// <returns>Returns change set ID</returns>
	public override void Run()
	{
		base.Run();

		// get previously store change set value
		var prevChangeSetIdStr = Cmd.Run("cm", "find changeset \"where branch='main' and comment like '%Build Version%'\" \"order by date desc\" \"limit 1\" --format=\"{changesetid}\" --nototal");
		var prevChangeSetId = int.TryParse(prevChangeSetIdStr.output, out var id) ? id : 0;

		// get current change set number from plastic
		var changeSetStr = Cmd.Run("cm", "find changeset \"where branch='main'\" \"order by date desc\" \"limit 1\" --format=\"{changesetid}\" --nototal");
		var changeSetId = int.TryParse(changeSetStr.output, out id) ? id : 0;
		Logger.Log($"changesetid: {changeSetId}");

		if (prevChangeSetId != 0 && changeSetId == prevChangeSetId)
		{
			Logger.Log("No changes detected. Change set Ids are same");
			return;
		}

		ChangeLog = GetChangeLog(prevChangeSetId);
		BuildVersion = GetNewBumpedVersion(changeSetId + 1);

		if (string.IsNullOrEmpty(BuildVersion))
			throw new Exception($"{nameof(BuildVersion)} can not be null. Something went wrong.");

		ReplaceVersions(BuildVersion);
	}

	private static string[] GetChangeLog(int prevChangeSetId)
	{
		var raw = Cmd.Run("cm", $"log --from=cs:{prevChangeSetId} --csformat=\"{{comment}}\"").output;
		var array = raw.Split(Environment.NewLine).Reverse().ToArray();
		return array;
	}

	private static string GetNewBumpedVersion(int newChangeSetId)
	{
		var arr = GetVersionArray();
		arr[0]++;
		arr[1] = newChangeSetId;
		return string.Join(".", arr);
	}
}
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

		// get current change set number from plastic
		var changeSetStr = Cmd.Run("cm", "find changeset \"where branch='main'\" \"order by date desc\" \"limit 1\" --format=\"{changesetid}\" --nototal");
		var changeSetId = int.TryParse(changeSetStr.output, out var id) ? id : 0;
		Logger.Log($"changesetid: {changeSetId}");

		if (PreviousChangeSetId != 0 && changeSetId == PreviousChangeSetId)
		{
			Logger.Log("No changes detected. Change set Ids are same");
			return;
		}

		BuildVersion = GetNewBumpedVersion(changeSetId + 1);

		if (string.IsNullOrEmpty(BuildVersion))
			throw new Exception($"{nameof(BuildVersion)} can not be null. Something went wrong.");

		ReplaceVersions(BuildVersion);
	}

	private static string GetNewBumpedVersion(int newChangeSetId)
	{
		var arr = GetVersionArray();
		arr[0]++;
		arr[1] = newChangeSetId;
		return string.Join(".", arr);
	}
}
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

		if (PreviousChangeSetId != 0 && CurrentChangeSetId == PreviousChangeSetId)
		{
			Logger.Log($"No changes detected. Change set Ids are same '{CurrentChangeSetId}'");
			return;
		}

		BuildVersion = GetNewBumpedVersion(CurrentChangeSetId + 1);

		if (string.IsNullOrEmpty(BuildVersion))
			throw new Exception($"{nameof(BuildVersion)} can not be null. Something went wrong.");

		ReplaceVersions(BuildVersion);
	}

	private static string GetNewBumpedVersion(int newChangeSetId)
	{
		var arr = GetVersionArray();
		arr[0]++; // incremental bump
		arr[1] = newChangeSetId; // set as changeSetId
		return string.Join(".", arr);
	}
}
using Deployment.Misc;

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
		Console.WriteLine($"changesetid: {changeSetId}");

		if (prevChangeSetId != 0 && changeSetId == prevChangeSetId)
		{
			Console.WriteLine("No changes detected. Change set Ids are same");
			return;
		}
		
		ChangeLog = Cmd.Run("cm", $"log --from=cs:{prevChangeSetId} --csformat=\"{{comment}}\"").output;
		BuildVersion  = GetNewBumpedVersion(changeSetId + 1);

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
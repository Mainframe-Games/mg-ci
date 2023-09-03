using Deployment.Configs;
using Deployment.Deployments;
using SharedLib;
using SharedLib.Server;

namespace Server.RemoteDeploy;

public class RemoteAppleDeploy : IProcessable
{
	public string? WorkspaceName { get; set; }
	public XcodeConfig? Config { get; set; }
	
	public ServerResponse Process()
	{
		var workspace = Workspace.GetWorkspaceFromName(WorkspaceName);
		var buildSettingsAsset = workspace.GetBuildTarget(BuildTargetFlag.iOS.ToString());
		var productName = buildSettingsAsset.GetValue<string>("ProductName");
		var buildPath = buildSettingsAsset.GetValue<string>("BuildPath");
		var workingDir = Path.Combine(buildPath, productName);
		var exportOptionPlist = $"{workspace.Directory}/BuildScripts/ios/exportOptions.plist";

		if (!File.Exists(exportOptionPlist))
			throw new FileNotFoundException(exportOptionPlist);

		if (Config?.AppleId is null)
			throw new NullReferenceException($"{nameof(Config.AppleId)} can not be null");
		if (Config?.AppSpecificPassword is null)
			throw new NullReferenceException($"{nameof(Config.AppSpecificPassword)} can not be null");
		
		XcodeDeploy.Deploy(
			workingDir,
			Config.AppleId,
			Config.AppSpecificPassword,
			exportOptionPlist);
		
		return ServerResponse.Ok;
	}
}
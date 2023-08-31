namespace SharedLib.BuildToDiscord;

public enum PipelineStage
{
	PreBuild,
	Build,
	Deploy,
	PostBuild
}

public enum BuildTaskStatus
{
	Pending,
	Succeed,
	Failed
}
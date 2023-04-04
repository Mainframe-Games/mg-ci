using Deployment;
using Deployment.Configs;

namespace Tests;

public class Tests
{
	private const string UNITY_VERSION = "2021.3.19f1";

	[SetUp]
	public void Setup()
	{
		var dir = new DirectoryInfo(@"..\..\..\..\Unity\BuildTest");
		if (dir.Exists)
			Environment.CurrentDirectory = dir.FullName;
	}

	[Test]
	public async Task UnityBuildSuccess()
	{
		var targetConfig = new TargetConfig
		{
			Target = UnityTarget.Win64,
			BuildPath = "Builds/win64",
			Settings = "BuildSettings_Success"
		};

		var unity = new LocalUnityBuild(UNITY_VERSION);
		await unity.Build(targetConfig);
		Assert.That(unity.Errors, Is.Null);
	}

	[Test]
	public async Task UnityBuildFailures()
	{
		var targetConfig = new TargetConfig
		{
			Target = UnityTarget.Win64,
			BuildPath = "Builds/win64",
			Settings = "BuildSettings_Failure"
		};

		var unity = new LocalUnityBuild(UNITY_VERSION);
		await unity.Build(targetConfig);
		Assert.That(unity.Errors, Is.Not.Null);
	}
}
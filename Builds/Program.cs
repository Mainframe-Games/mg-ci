// See https://aka.ms/new-console-template for more information

using Builder;
using Deployment.Configs;
using SharedLib;

var currentWorkspace = Workspace.GetWorkspace();
Logger.Log($"Chosen workspace: {currentWorkspace}");
// var pipe = new BuildPipeline(currentWorkspace, args);
// await pipe.RunAsync();

var rootDir = new DirectoryInfo(currentWorkspace.Directory);
var config = BuildConfig.GetConfig(currentWorkspace.Directory);
var targets = await ClonesManager.CloneProject(rootDir, config);

Logger.Log("Program End");

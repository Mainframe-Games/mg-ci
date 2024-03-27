using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.Data.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServerClientShared;

namespace AvaloniaAppMVVM.ViewModels;

public partial class BuildTargetsViewModel : ViewModelBase
{
    public Project Project { get; set; }

    [IgnoreDataMember]
    public static ObservableCollection<string> ExtensionOptions { get; } =
        [".exe", ".app", ".x86_64", ".apk", "/"];

    [IgnoreDataMember]
    public static ObservableCollection<Unity.BuildTarget> BuildTargetOptions { get; } =
        new(Enum.GetValues<Unity.BuildTarget>());
    [IgnoreDataMember]
    public static ObservableCollection<Unity.BuildTargetGroup> BuildTargetGroupOptions { get; } =
        new(Enum.GetValues<Unity.BuildTargetGroup>());

    [IgnoreDataMember]
    public static ObservableCollection<Unity.SubTarget> SubTargetOptions { get; } =
        new(Enum.GetValues<Unity.SubTarget>());

    [IgnoreDataMember]
    public static ObservableCollection<Unity.BuildOptions> BuildOptionOptions { get; } =
        new(Enum.GetValues<Unity.BuildOptions>());

    public ObservableCollection<UnityBuildTargetTemplate> BuildTargets { get; } = [];
    public ObservableCollection<NewBuildTargetTemplate> NewTargetTemplates { get; } =
        [
            new NewBuildTargetTemplate(
                "Windows",
                Unity.BuildTarget.StandaloneWindows64,
                Unity.BuildTargetGroup.Standalone,
                ".exe"
            ),
            new NewBuildTargetTemplate(
                "Mac",
                Unity.BuildTarget.StandaloneOSX,
                Unity.BuildTargetGroup.Standalone,
                ".app"
            ),
            new NewBuildTargetTemplate(
                "Linux",
                Unity.BuildTarget.StandaloneLinux64,
                Unity.BuildTargetGroup.Standalone,
                ".x86_64"
            ),
            new NewBuildTargetTemplate(
                "Android",
                Unity.BuildTarget.Android,
                Unity.BuildTargetGroup.Android,
                ".apk"
            ),
            new NewBuildTargetTemplate(
                "iOS",
                Unity.BuildTarget.iOS,
                Unity.BuildTargetGroup.iOS,
                "/"
            ),
        ];

    [ObservableProperty]
    private UnityBuildTargetTemplate? _selectedBuildTarget;

    [ObservableProperty]
    private NewBuildTargetTemplate? _selectedNewTargetTemplate;

    [ObservableProperty]
    private bool _showContent;

    [ObservableProperty]
    private bool _showError = true;

    partial void OnSelectedBuildTargetChanged(UnityBuildTargetTemplate? value)
    {
        ShowContent = true;
        ShowError = false;
    }

    [RelayCommand]
    public void NewTargetCommand(string name)
    {
        var template = NewTargetTemplates.FirstOrDefault(template => template.Name == name);

        if (template is null)
            return;

        var data = new UnityBuildTarget
        {
            Name = template.Name,
            ProductName = Project.Settings.ProjectName,
            BuildPath = $"Builds/{template.Name}",
            Target = template.Target,
            SubTarget = Unity.SubTarget.Player,
            TargetGroup = template.TargetGroup,
            Extension = template.Extension
        };

        var newTarget = new UnityBuildTargetTemplate(data);
        BuildTargets.Add(newTarget);
        SelectedBuildTarget = newTarget;
    }

    [RelayCommand]
    public void DeleteTargetCommand(string name)
    {
        foreach (var target in BuildTargets)
        {
            if (target.Data.Name != name)
                continue;

            BuildTargets.Remove(target);
            break;
        }

        if (BuildTargets.Count > 0)
        {
            SelectedBuildTarget = BuildTargets[0];
        }
        else
        {
            ShowContent = false;
            ShowError = true;
        }
    }
}

public class NewBuildTargetTemplate(
    string? name,
    Unity.BuildTarget target,
    Unity.BuildTargetGroup targetGroup,
    string? extension
)
{
    public string? Name { get; set; } = name;
    public Unity.BuildTarget Target { get; set; } = target;
    public Unity.BuildTargetGroup TargetGroup { get; set; } = targetGroup;
    public string? Extension { get; set; } = extension;
}

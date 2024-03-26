using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using ServerClientShared;

namespace AvaloniaAppMVVM.Data.Shared;

/// <summary>
/// Unity Build Target, synced with <see cref="ServerClientShared.UnityBuildTarget"/>
/// </summary>
public class UnityBuildTargetTemplate : CiProcess
{
    public UnityBuildTarget Data { get; set; } = new();

    public UnityBuildTargetTemplate() { }

    public UnityBuildTargetTemplate(UnityBuildTarget data)
    {
        Data = data;
    }

    [IgnoreDataMember]
    public ObservableCollection<string> ExtensionOptions { get; } =
        [".exe", ".app", ".x86_64", ".apk", "/"];

    [IgnoreDataMember]
    public ObservableCollection<string> BuildTargetOptions { get; } =
        new(Enum.GetNames(typeof(Unity.BuildTarget)));

    [IgnoreDataMember]
    public ObservableCollection<string> BuildTargetGroupOptions { get; } =
        new(Enum.GetNames(typeof(Unity.BuildTargetGroup)));

    [IgnoreDataMember]
    public ObservableCollection<string> SubTargetOptions { get; } =
        new(Enum.GetNames(typeof(Unity.SubTarget)));

    [IgnoreDataMember]
    public ObservableCollection<string> BuildOptionOptions { get; } =
        new(Enum.GetNames(typeof(Unity.BuildOptions)));
}

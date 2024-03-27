using ServerClientShared;

namespace AvaloniaAppMVVM.Data.Shared;

/// <summary>
/// Unity Build Target, synced with <see cref="ServerClientShared.UnityBuildTarget"/>
/// </summary>
public class UnityBuildTargetTemplate(UnityBuildTarget data) : CiProcess
{
    public UnityBuildTarget Data { get; set; } = data;
}

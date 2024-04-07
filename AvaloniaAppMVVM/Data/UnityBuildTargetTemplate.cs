using AvaloniaAppMVVM.Utils;

namespace AvaloniaAppMVVM.Data.Shared;

/// <summary>
/// Unity Build Target, synced with <see cref="UnityBuildTarget"/>
/// </summary>
public class UnityBuildTargetTemplate(UnityBuildTarget data) : CiProcess
{
    public UnityBuildTarget Data { get; set; } = data;
}

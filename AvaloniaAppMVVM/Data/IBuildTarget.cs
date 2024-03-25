namespace AvaloniaAppMVVM.Data;

public interface IBuildTarget : IProcess
{
    public string? Name { get; set; }
}
namespace AvaloniaAppMVVM.Data;

public interface IProcess
{
    public bool IsBusy { get; set; }
    public bool IsQueued { get; set; }
    public bool Succeeded { get; set; }
    public bool Failed { get; set; }
    public string? Id { get; set; }
    public string? Logs{get; set;}
}

public class CiProcess : IProcess
{
    public bool IsBusy { get; set; }
    public bool IsQueued { get; set; }
    public bool Succeeded { get; set; }
    public bool Failed { get; set; }
    public string? Id { get; set; }
    public string? Logs { get; set; }
    
    public List<IProcess>? SubProcesses { get; set; }
}

using System.Numerics;
using ImGuiNET;

namespace GuiApp;

public class ActiveWorkspace
{
    private readonly string _workspaceName;
    
    public ActiveWorkspace(string worksapceName)
    {
        _workspaceName = worksapceName;
    }

    public void DrawGui()
    {
        var windowSize = ImGui.GetIO().DisplaySize;
        ImGui.SetNextWindowSize(windowSize);
        ImGui.SetNextWindowPos(new Vector2(0, ImGui.GetCursorPosY()));
        
        ImGui.Begin("Main", ImGuiWindowFlags.NoCollapse 
                            | ImGuiWindowFlags.NoTitleBar
                            | ImGuiWindowFlags.NoResize
                            | ImGuiWindowFlags.NoMove
                            | ImGuiWindowFlags.NoBackground);
        { 
            ImGui.Text($"Workspace Name: {_workspaceName}");
        }
        ImGui.End();
    }
}
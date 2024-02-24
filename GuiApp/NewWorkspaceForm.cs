using System.Numerics;
using ImGuiNET;

namespace GuiApp;

public class NewWorkspaceForm
{
    public static event Action<NewWorkspaceForm>? OnFormCompleted;
    public string ProjectName = string.Empty;
    
    private readonly Vector2 _windowSize = new(500, 500);

    public void DrawGui()
    {
        ImGui.SetNextWindowSize(_windowSize);
        ImGui.SetNextWindowPos((ImGui.GetIO().DisplaySize - _windowSize) / 2f);
        ImGui.Begin("New Workspace", ImGuiWindowFlags.NoCollapse
                                     | ImGuiWindowFlags.NoResize
                                     | ImGuiWindowFlags.NoMove 
                                     | ImGuiWindowFlags.AlwaysAutoResize);
        {
            ImGui.InputText("Project Name", ref ProjectName, 100);

            if (ImGui.Button("Save"))
                OnFormCompleted?.Invoke(this);
        }
        ImGui.End();
    }
}
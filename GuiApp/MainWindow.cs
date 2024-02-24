using ImGuiNET;

namespace GuiApp;

public class MainWindow
{
    private NewWorkspaceForm? _newWorkspaceForm;
    private ActiveWorkspace? _activeWorkspace;

    public MainWindow()
    {
        NewWorkspaceForm.OnFormCompleted += OnNewWorkspaceFormCompleted;
    }

    private void OnNewWorkspaceFormCompleted(NewWorkspaceForm form)
    {
        Console.WriteLine($"New Project name: {form.ProjectName}");
        _newWorkspaceForm = null;
        _activeWorkspace = new ActiveWorkspace(form.ProjectName);
    }

    public void DrawGui()
    {
        DrawMainMenuBar();
        
        _newWorkspaceForm?.DrawGui();
        _activeWorkspace?.DrawGui();
    }

    private void DrawMainMenuBar()
    {
        if (ImGui.BeginMainMenuBar())
        {
            // Add menus to the main menu bar
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New", "Ctrl+N"))
                {
                    _newWorkspaceForm = new NewWorkspaceForm();
                }
        
                if (ImGui.MenuItem("Open", "Ctrl+O"))
                {
                    Console.WriteLine("Open file");
                }
        
                if (ImGui.MenuItem("Save", "Ctrl+S"))
                {
                    Console.WriteLine("Safe file");
                }
        
                ImGui.EndMenu();
            }
            
            ImGui.EndMainMenuBar();
        }
    }
}
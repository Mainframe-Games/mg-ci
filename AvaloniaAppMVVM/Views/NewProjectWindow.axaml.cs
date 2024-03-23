using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaAppMVVM.Forms;
using FluentAvalonia.UI.Windowing;

namespace AvaloniaAppMVVM.Views;

public partial class NewProjectWindow : AppWindow
{
    private readonly NewProjectForm _form = new();

    public NewProjectWindow()
    {
        InitializeComponent();
    }
}

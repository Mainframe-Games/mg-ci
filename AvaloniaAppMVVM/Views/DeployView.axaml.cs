using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaAppMVVM.Data;
using AvaloniaAppMVVM.ViewModels;

namespace AvaloniaAppMVVM.Views;

public partial class DeployView : MyUserControl<DeployViewModel>
{
    public DeployView()
    {
        InitializeComponent();
    }

    protected override void OnInit()
    {
        _viewModel.Project = _project;

        foreach (var appBuild in _project.Deployment.SteamAppBuilds)
            AddNewSteamAppBuild(appBuild.AppID, appBuild.DepotIds);
    }

    protected override void OnPreSave()
    {
        _project.Deployment.SteamAppBuilds.Clear();
        // foreach (var steamVdf in _viewModel.SteamVdfs)
        // _project.Deployment.SteamVdfs.Add(steamVdf.Value);
    }

    private void Button_DeleteSteamVdf_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: StringWrap vdf })
            return;

        // if (DataContext is DeployViewModel vm)
        //     vm.SteamVdfs.Remove(vdf);
    }

    private void Button_AddSteamAppBuild_OnClick(object? sender, RoutedEventArgs e)
    {
        AddNewSteamAppBuild(string.Empty, []);
    }

    private void AddNewSteamAppBuild(string appBuildId, List<string> depotIds)
    {
        var b = new _AppBuildTemplate(new AppBuild { AppID = appBuildId, DepotIds = depotIds });
        b.OnDeleteClick += s =>
        {
            AppBuildsStackPanel.Children.Remove(s);
        };
        AppBuildsStackPanel.Children.Add(b);
    }

    private class _TextDeletableItem : Grid
    {
        public event Action<_TextDeletableItem>? OnDeleteClick;

        public _TextDeletableItem(string text)
        {
            ColumnDefinitions = new ColumnDefinitions("*, Auto");
            Children.Add(new TextBox { Text = text });

            var btn = new Button { Content = "Delete", Background = Brushes.Firebrick };
            Children.Add(btn);

            for (var i = 0; i < Children.Count; i++)
            {
                var gridChild = Children[i];
                SetColumn(gridChild, i);
            }

            btn.Click += (sender, args) => OnDeleteClick?.Invoke(this);
        }
    }

    private class _SteamDepotItem : StackPanel
    {
        public TextBox TextBox { get; set; }
        public ComboBox ComboBox { get; set; }
        public Button DeleteButton { get; set; }

        public _SteamDepotItem(string text)
        {
            Orientation = Orientation.Horizontal;

            TextBox = new TextBox { Text = text };
            ComboBox = new ComboBox { ItemsSource = new[] { "1", "2", "3" }, SelectedIndex = 0 };
            DeleteButton = new Button { Content = "Delete" };
            Children.Add(TextBox);
            Children.Add(ComboBox);
            Children.Add(DeleteButton);
        }
    }

    private class _AppBuildTemplate : StackPanel
    {
        public TextBlock AppId { get; set; } = new() { Text = "App ID:" };
        public _TextDeletableItem AppIdTextBox { get; set; }
        public TextBlock DepotIds { get; set; } = new() { Text = "Depot IDs:" };
        public List<_SteamDepotItem> DepotIdsTextBoxes2 { get; set; } = [];

        public event Action<_AppBuildTemplate>? OnDeleteClick;

        public _AppBuildTemplate(AppBuild appBuild)
        {
            AppIdTextBox = new _TextDeletableItem(appBuild.AppID);
            AppIdTextBox.OnDeleteClick += OnDeleteClicked;
            foreach (var depotId in appBuild.DepotIds)
                DepotIdsTextBoxes2.Add(new _SteamDepotItem(depotId));

            Children.Add(AppId);
            Children.Add(AppIdTextBox);

            Children.Add(DepotIds);
            foreach (var depotId in DepotIdsTextBoxes2)
                Children.Add(depotId);
        }

        private void OnDeleteClicked(_TextDeletableItem textDeletableItem)
        {
            OnDeleteClick?.Invoke(this);
        }
    }
}

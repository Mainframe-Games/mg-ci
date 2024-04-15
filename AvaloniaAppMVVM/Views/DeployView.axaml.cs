using Avalonia;
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
            AddNewSteamAppBuild(appBuild.AppID, appBuild.Depots);
    }

    protected override void OnPreSave()
    {
        _project.Deployment.SteamAppBuilds.Clear();

        foreach (var child in AppBuildsStackPanel.Children)
        {
            if (child is not _AppBuildTemplate template)
                continue;

            var depots = new List<Depot>();
            foreach (var depot in template.DepotsStackPanel.Children)
            {
                if (depot is not _SteamDepotItem steamDepot)
                    continue;

                depots.Add(
                    new Depot
                    {
                        Id = steamDepot.TextBox.Text!,
                        BuildTargetName =
                            steamDepot.ComboBox.SelectedItem?.ToString() ?? string.Empty
                    }
                );
            }

            _project.Deployment.SteamAppBuilds.Add(
                new AppBuild { AppID = template.AppIdTextBox.Text, Depots = depots }
            );
        }
    }

    private void Button_AddSteamAppBuild_OnClick(object? sender, RoutedEventArgs e)
    {
        AddNewSteamAppBuild(string.Empty, []);
    }

    private void AddNewSteamAppBuild(string appBuildId, List<Depot> depots)
    {
        var appBuild = new AppBuild { AppID = appBuildId, Depots = depots };
        var buildTargetOptions = _project.BuildTargets.Select(x => x.Name).ToList();
        var template = new _AppBuildTemplate(appBuild, buildTargetOptions!);
        template.OnDeleteClick += aBuild =>
        {
            AppBuildsStackPanel.Children.Remove(aBuild);
        };
        AppBuildsStackPanel.Children.Add(template);
    }

    private class _TextDeletableItem : Grid
    {
        public event Action<_TextDeletableItem>? OnDeleteClick;

        public string Text => ((TextBox)Children[0]).Text!;

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

        public _SteamDepotItem(
            string text,
            string selectedBuildTarget,
            IList<string> buildTargetOptions
        )
        {
            Orientation = Orientation.Horizontal;
            Spacing = 10;
            Margin = new Thickness(10, 5, 5, 10);

            TextBox = new TextBox { Text = text };
            var buildTargetIndex = Math.Max(buildTargetOptions.IndexOf(selectedBuildTarget), 0);
            ComboBox = new ComboBox
            {
                ItemsSource = buildTargetOptions,
                SelectedIndex = buildTargetIndex
            };
            DeleteButton = new Button { Content = "Delete", Background = Brushes.Firebrick };
            Children.Add(TextBox);
            Children.Add(ComboBox);
            Children.Add(DeleteButton);
        }
    }

    private class _AppBuildTemplate : StackPanel
    {
        public TextBlock AppId { get; set; } = new() { Text = "App ID:" };
        public _TextDeletableItem AppIdTextBox { get; set; }

        public StackPanel DepotsStackPanel { get; }
        public TextBlock DepotIds { get; set; } = new() { Text = "Depot IDs:" };

        public event Action<_AppBuildTemplate>? OnDeleteClick;

        public _AppBuildTemplate(AppBuild appBuild, IList<string> buildTargetOptions)
        {
            AppIdTextBox = new _TextDeletableItem(appBuild.AppID);
            AppIdTextBox.OnDeleteClick += OnDeleteClicked;

            Children.Add(AppId);
            Children.Add(AppIdTextBox);

            DepotsStackPanel = new StackPanel { Spacing = 10 };
            Children.Add(DepotsStackPanel);

            DepotsStackPanel.Children.Add(DepotIds);
            foreach (var depot in appBuild.Depots)
                DepotsStackPanel.Children.Add(
                    new _SteamDepotItem(depot.Id, depot.BuildTargetName, buildTargetOptions)
                );

            // depot add button
            var addDepotIdButton = new Button { Content = "Add Depot" };
            addDepotIdButton.Click += (sender, args) =>
            {
                var depot = new _SteamDepotItem(string.Empty, string.Empty, buildTargetOptions);
                DepotsStackPanel.Children.Add(depot);
            };
            Children.Add(addDepotIdButton);
        }

        private void OnDeleteClicked(_TextDeletableItem textDeletableItem)
        {
            OnDeleteClick?.Invoke(this);
        }
    }
}

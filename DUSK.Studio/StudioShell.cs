namespace DUSK.Studio;

using DUSK.Core;
using DUSK.Engine;
using DUSK.UI;
using DUSK.Theme;

/// <summary>
/// Main Studio shell for visual development.
/// Provides IDE-like interface for designing DUSK scenes.
/// </summary>
public class StudioShell : SceneBase
{
    private SceneGraphEditor? _sceneGraph;
    private VisualEditor? _visualEditor;
    private UIForm? _toolbox;
    private UIForm? _properties;
    private IScene? _editingScene;

    public IScene? EditingScene => _editingScene;
    public event EventHandler<SceneEventArgs>? SceneSaved;
    public event EventHandler<SceneEventArgs>? SceneLoaded;

    public StudioShell() : base("studio-shell", "DUSK Studio")
    {
        BackgroundColor = new DuskColor(45, 45, 48);
    }

    protected override void OnInitialize()
    {
        Theme = ThemeManager.Instance.CurrentTheme;

        // Create main layout
        var mainLayout = new UIForm("main-layout")
        {
            Bounds = Bounds,
            LayoutMode = LayoutMode.Horizontal,
            ShowBorder = false,
            ShowTitle = false
        };

        // Left panel - Scene Graph
        _sceneGraph = new SceneGraphEditor
        {
            Bounds = new DuskRect(0, 0, 250, Bounds.Height)
        };

        // Center - Visual Editor
        _visualEditor = new VisualEditor
        {
            Bounds = new DuskRect(250, 0, Bounds.Width - 500, Bounds.Height)
        };

        // Right panel - Properties
        _properties = new UIForm("Properties")
        {
            Bounds = new DuskRect(Bounds.Width - 250, 0, 250, Bounds.Height),
            LayoutMode = LayoutMode.Vertical,
            BackgroundColor = new DuskColor(37, 37, 38)
        };

        // Toolbox at top
        _toolbox = CreateToolbox();

        AddElement(mainLayout);
        mainLayout.AddChild(_sceneGraph);
        mainLayout.AddChild(_visualEditor);
        mainLayout.AddChild(_properties);
        AddElement(_toolbox);

        _sceneGraph.SelectionChanged += OnSceneGraphSelectionChanged;
    }

    private UIForm CreateToolbox()
    {
        var toolbox = new UIForm
        {
            Bounds = new DuskRect(0, 0, Bounds.Width, 40),
            LayoutMode = LayoutMode.Horizontal,
            ShowBorder = false,
            BackgroundColor = new DuskColor(51, 51, 55)
        };

        var btnNew = new UIButton("New") { Bounds = new DuskRect(5, 5, 60, 30) };
        var btnOpen = new UIButton("Open") { Bounds = new DuskRect(70, 5, 60, 30) };
        var btnSave = new UIButton("Save") { Bounds = new DuskRect(135, 5, 60, 30) };
        var btnPreview = new UIButton("Preview") { Bounds = new DuskRect(200, 5, 70, 30) };

        btnNew.Click += (_, _) => NewScene();
        btnOpen.Click += (_, _) => OpenScene();
        btnSave.Click += (_, _) => SaveScene();
        btnPreview.Click += (_, _) => PreviewScene();

        toolbox.AddChild(btnNew);
        toolbox.AddChild(btnOpen);
        toolbox.AddChild(btnSave);
        toolbox.AddChild(btnPreview);

        return toolbox;
    }

    public void NewScene()
    {
        _editingScene = new SceneBase(null, "Untitled Scene");
        _editingScene.Initialize();
        _sceneGraph?.SetScene(_editingScene);
        _visualEditor?.SetScene(_editingScene);
    }

    public void OpenScene()
    {
        // Would open file dialog
    }

    public void SaveScene()
    {
        if (_editingScene != null)
        {
            SceneSaved?.Invoke(this, new SceneEventArgs(_editingScene));
        }
    }

    public void PreviewScene()
    {
        // Would launch preview window
    }

    public void LoadScene(IScene scene)
    {
        _editingScene = scene;
        _sceneGraph?.SetScene(scene);
        _visualEditor?.SetScene(scene);
        SceneLoaded?.Invoke(this, new SceneEventArgs(scene));
    }

    private void OnSceneGraphSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _visualEditor?.SelectElement(e.SelectedElement);
        UpdatePropertiesPanel(e.SelectedElement);
    }

    private void UpdatePropertiesPanel(IUIElement? element)
    {
        if (_properties == null) return;

        // Clear existing properties
        foreach (var child in _properties.Children.ToList())
        {
            _properties.RemoveChild(child);
        }

        if (element == null) return;

        // Add property editors
        var nameLabel = new UIText("Name:") { IsEditable = false };
        var nameInput = new UIText(element.Name) { IsEditable = true };

        var xLabel = new UIText("X:") { IsEditable = false };
        var xInput = new UIText(element.Bounds.X.ToString()) { IsEditable = true };

        var yLabel = new UIText("Y:") { IsEditable = false };
        var yInput = new UIText(element.Bounds.Y.ToString()) { IsEditable = true };

        _properties.AddChild(nameLabel);
        _properties.AddChild(nameInput);
        _properties.AddChild(xLabel);
        _properties.AddChild(xInput);
        _properties.AddChild(yLabel);
        _properties.AddChild(yInput);

        _properties.PerformLayout();
    }
}

public class SelectionChangedEventArgs : EventArgs
{
    public IUIElement? SelectedElement { get; }

    public SelectionChangedEventArgs(IUIElement? element)
    {
        SelectedElement = element;
    }
}

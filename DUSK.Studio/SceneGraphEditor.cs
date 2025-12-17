namespace DUSK.Studio;

using DUSK.Core;
using DUSK.Engine;
using DUSK.UI;

/// <summary>
/// Tree view of scene hierarchy.
/// Shows all elements in the current scene.
/// </summary>
public class SceneGraphEditor : UIElementBase
{
    private IScene? _scene;
    private IUIElement? _selectedElement;
    private readonly List<TreeNode> _nodes = new();
    private int _scrollOffset;

    public IUIElement? SelectedElement => _selectedElement;
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    public SceneGraphEditor() : base("scene-graph")
    {
        PaddingLeft = 8;
        PaddingTop = 30;
    }

    public void SetScene(IScene? scene)
    {
        _scene = scene;
        RebuildTree();
    }

    public void SelectElement(IUIElement? element)
    {
        if (_selectedElement == element) return;

        _selectedElement = element;
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(element));
    }

    private void RebuildTree()
    {
        _nodes.Clear();
        if (_scene == null) return;

        // Add scene root
        var rootNode = new TreeNode(_scene.Title, 0, null);
        _nodes.Add(rootNode);

        // Add all elements
        foreach (var element in _scene.Elements)
        {
            AddElementToTree(element, 1);
        }
    }

    private void AddElementToTree(IUIElement element, int depth)
    {
        var node = new TreeNode(
            $"{element.GetType().Name}: {element.Name}",
            depth,
            element
        );
        _nodes.Add(node);

        foreach (var child in element.Children)
        {
            AddElementToTree(child, depth + 1);
        }
    }

    protected override void OnRender(IRenderer renderer)
    {
        // Draw background
        renderer.DrawRectangle(Bounds, new DuskColor(37, 37, 38));

        // Draw header
        renderer.DrawRectangle(
            new DuskRect(Bounds.X, Bounds.Y, Bounds.Width, 25),
            new DuskColor(51, 51, 55)
        );
        renderer.DrawText("Scene Graph", new DuskPoint(Bounds.X + 8, Bounds.Y + 5),
            DuskFont.DefaultBold, DuskColor.White);

        // Draw tree nodes
        var y = Bounds.Y + PaddingTop - _scrollOffset;
        foreach (var node in _nodes)
        {
            if (y < Bounds.Y + 25 || y > Bounds.Bottom - 20)
            {
                y += 20;
                continue;
            }

            var x = Bounds.X + PaddingLeft + node.Depth * 15;

            // Draw selection highlight
            if (node.Element == _selectedElement)
            {
                renderer.DrawRectangle(
                    new DuskRect(Bounds.X, y - 2, Bounds.Width, 20),
                    new DuskColor(0, 120, 215)
                );
            }

            // Draw expand/collapse icon
            if (node.Element?.Children.Count > 0)
            {
                renderer.DrawText(node.IsExpanded ? "-" : "+",
                    new DuskPoint(x - 12, y), DuskFont.Default, DuskColor.White);
            }

            // Draw node text
            var textColor = node.Element == _selectedElement ? DuskColor.White : new DuskColor(200, 200, 200);
            renderer.DrawText(node.Text, new DuskPoint(x, y), DuskFont.Default, textColor);

            y += 20;
        }
    }

    public override void HandleMouseDown(MouseEventArgs args)
    {
        base.HandleMouseDown(args);

        // Find clicked node
        var y = Bounds.Y + PaddingTop - _scrollOffset;
        foreach (var node in _nodes)
        {
            if (args.Position.Y >= y && args.Position.Y < y + 20)
            {
                SelectElement(node.Element);
                break;
            }
            y += 20;
        }
    }

    private class TreeNode
    {
        public string Text { get; }
        public int Depth { get; }
        public IUIElement? Element { get; }
        public bool IsExpanded { get; set; } = true;

        public TreeNode(string text, int depth, IUIElement? element)
        {
            Text = text;
            Depth = depth;
            Element = element;
        }
    }
}

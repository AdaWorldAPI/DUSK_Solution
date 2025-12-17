namespace DUSK.Studio;

using DUSK.Core;
using DUSK.Engine;
using DUSK.UI;

/// <summary>
/// Visual WYSIWYG editor for scene design.
/// Renders the scene and allows drag-and-drop element placement.
/// </summary>
public class VisualEditor : UIElementBase
{
    private IScene? _scene;
    private IUIElement? _selectedElement;
    private bool _isDragging;
    private DuskPoint _dragStart;
    private DuskPoint _elementStartPos;
    private ResizeHandle _activeHandle = ResizeHandle.None;

    private const int HandleSize = 8;
    private const int GridSize = 10;

    public bool ShowGrid { get; set; } = true;
    public bool SnapToGrid { get; set; } = true;
    public float Zoom { get; set; } = 1.0f;

    public event EventHandler<ElementMovedEventArgs>? ElementMoved;
    public event EventHandler<ElementResizedEventArgs>? ElementResized;

    public VisualEditor() : base("visual-editor")
    {
    }

    public void SetScene(IScene? scene)
    {
        _scene = scene;
        _selectedElement = null;
    }

    public void SelectElement(IUIElement? element)
    {
        _selectedElement = element;
    }

    protected override void OnRender(IRenderer renderer)
    {
        // Draw editor background
        renderer.DrawRectangle(Bounds, new DuskColor(30, 30, 30));

        // Draw grid
        if (ShowGrid)
        {
            DrawGrid(renderer);
        }

        // Draw canvas area
        var canvasBounds = GetCanvasBounds();
        renderer.DrawRectangle(canvasBounds, DuskColor.White);

        // Render scene preview
        if (_scene != null)
        {
            renderer.SetClipRegion(canvasBounds);
            _scene.Render(renderer);
            renderer.SetClipRegion(null);
        }

        // Draw selection handles
        if (_selectedElement != null)
        {
            DrawSelectionHandles(renderer, _selectedElement);
        }
    }

    private void DrawGrid(IRenderer renderer)
    {
        var gridColor = new DuskColor(50, 50, 50);
        var scaledGrid = (int)(GridSize * Zoom);

        for (int x = Bounds.X; x < Bounds.Right; x += scaledGrid)
        {
            renderer.DrawLine(
                new DuskPoint(x, Bounds.Y),
                new DuskPoint(x, Bounds.Bottom),
                gridColor
            );
        }

        for (int y = Bounds.Y; y < Bounds.Bottom; y += scaledGrid)
        {
            renderer.DrawLine(
                new DuskPoint(Bounds.X, y),
                new DuskPoint(Bounds.Right, y),
                gridColor
            );
        }
    }

    private DuskRect GetCanvasBounds()
    {
        var margin = 50;
        return new DuskRect(
            Bounds.X + margin,
            Bounds.Y + margin,
            (int)((Bounds.Width - margin * 2) * Zoom),
            (int)((Bounds.Height - margin * 2) * Zoom)
        );
    }

    private void DrawSelectionHandles(IRenderer renderer, IUIElement element)
    {
        var bounds = TransformToCanvas(element.Bounds);
        var handleColor = new DuskColor(0, 120, 215);

        // Draw selection border
        renderer.DrawRectangle(bounds.Inflate(1, 1), handleColor, filled: false);

        // Draw resize handles
        var handles = GetHandleRects(bounds);
        foreach (var handle in handles)
        {
            renderer.DrawRectangle(handle, handleColor, filled: true);
        }
    }

    private DuskRect[] GetHandleRects(DuskRect bounds)
    {
        var half = HandleSize / 2;
        return new[]
        {
            new DuskRect(bounds.X - half, bounds.Y - half, HandleSize, HandleSize), // TL
            new DuskRect(bounds.X + bounds.Width / 2 - half, bounds.Y - half, HandleSize, HandleSize), // T
            new DuskRect(bounds.Right - half, bounds.Y - half, HandleSize, HandleSize), // TR
            new DuskRect(bounds.Right - half, bounds.Y + bounds.Height / 2 - half, HandleSize, HandleSize), // R
            new DuskRect(bounds.Right - half, bounds.Bottom - half, HandleSize, HandleSize), // BR
            new DuskRect(bounds.X + bounds.Width / 2 - half, bounds.Bottom - half, HandleSize, HandleSize), // B
            new DuskRect(bounds.X - half, bounds.Bottom - half, HandleSize, HandleSize), // BL
            new DuskRect(bounds.X - half, bounds.Y + bounds.Height / 2 - half, HandleSize, HandleSize) // L
        };
    }

    public override void HandleMouseDown(MouseEventArgs args)
    {
        base.HandleMouseDown(args);

        if (_selectedElement != null)
        {
            // Check resize handles
            var bounds = TransformToCanvas(_selectedElement.Bounds);
            var handles = GetHandleRects(bounds);
            var handleTypes = new[] {
                ResizeHandle.TopLeft, ResizeHandle.Top, ResizeHandle.TopRight,
                ResizeHandle.Right, ResizeHandle.BottomRight, ResizeHandle.Bottom,
                ResizeHandle.BottomLeft, ResizeHandle.Left
            };

            for (int i = 0; i < handles.Length; i++)
            {
                if (handles[i].Contains(args.Position))
                {
                    _activeHandle = handleTypes[i];
                    _isDragging = true;
                    _dragStart = args.Position;
                    _elementStartPos = new DuskPoint(_selectedElement.Bounds.X, _selectedElement.Bounds.Y);
                    return;
                }
            }
        }

        // Check for element selection
        if (_scene != null)
        {
            var canvasPoint = TransformFromCanvas(args.Position);
            foreach (var element in _scene.Elements.Reverse())
            {
                if (element.HitTest(canvasPoint))
                {
                    SelectElement(element);
                    _isDragging = true;
                    _dragStart = args.Position;
                    _elementStartPos = new DuskPoint(element.Bounds.X, element.Bounds.Y);
                    return;
                }
            }
        }

        SelectElement(null);
    }

    public override void HandleMouseUp(MouseEventArgs args)
    {
        base.HandleMouseUp(args);

        if (_isDragging && _selectedElement != null)
        {
            ElementMoved?.Invoke(this, new ElementMovedEventArgs(_selectedElement, _selectedElement.Bounds.Location));
        }

        _isDragging = false;
        _activeHandle = ResizeHandle.None;
    }

    private DuskRect TransformToCanvas(DuskRect rect)
    {
        var canvas = GetCanvasBounds();
        return new DuskRect(
            canvas.X + (int)(rect.X * Zoom),
            canvas.Y + (int)(rect.Y * Zoom),
            (int)(rect.Width * Zoom),
            (int)(rect.Height * Zoom)
        );
    }

    private DuskPoint TransformFromCanvas(DuskPoint point)
    {
        var canvas = GetCanvasBounds();
        return new DuskPoint(
            (int)((point.X - canvas.X) / Zoom),
            (int)((point.Y - canvas.Y) / Zoom)
        );
    }

    private int SnapToGridValue(int value)
    {
        if (!SnapToGrid) return value;
        return (int)(Math.Round((double)value / GridSize) * GridSize);
    }
}

public enum ResizeHandle
{
    None,
    TopLeft, Top, TopRight,
    Left, Right,
    BottomLeft, Bottom, BottomRight
}

public class ElementMovedEventArgs : EventArgs
{
    public IUIElement Element { get; }
    public DuskPoint NewPosition { get; }

    public ElementMovedEventArgs(IUIElement element, DuskPoint newPosition)
    {
        Element = element;
        NewPosition = newPosition;
    }
}

public class ElementResizedEventArgs : EventArgs
{
    public IUIElement Element { get; }
    public DuskRect NewBounds { get; }

    public ElementResizedEventArgs(IUIElement element, DuskRect newBounds)
    {
        Element = element;
        NewBounds = newBounds;
    }
}

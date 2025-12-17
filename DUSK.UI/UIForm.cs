namespace DUSK.UI;

using DUSK.Core;

/// <summary>
/// Container element for grouping UI elements.
/// Supports various layout modes for child elements.
/// WinForms Panel/GroupBox equivalent.
/// </summary>
public class UIForm : UIElementBase
{
    private string _title = string.Empty;
    private LayoutMode _layoutMode = LayoutMode.Manual;
    private bool _showBorder = true;
    private bool _showTitle = true;

    public string Title
    {
        get => _title;
        set => _title = value ?? string.Empty;
    }

    public LayoutMode LayoutMode
    {
        get => _layoutMode;
        set
        {
            _layoutMode = value;
            PerformLayout();
        }
    }

    public bool ShowBorder
    {
        get => _showBorder;
        set => _showBorder = value;
    }

    public bool ShowTitle
    {
        get => _showTitle;
        set => _showTitle = value;
    }

    public int Spacing { get; set; } = 4;
    public DuskColor? BackgroundColor { get; set; }
    public DuskColor? BorderColor { get; set; }

    public UIForm(string? id = null) : base(id)
    {
        PaddingLeft = 8;
        PaddingRight = 8;
        PaddingTop = 8;
        PaddingBottom = 8;
    }

    public UIForm(string title, string? id = null) : this(id)
    {
        Title = title;
    }

    public override void AddChild(IUIElement child)
    {
        base.AddChild(child);
        PerformLayout();
    }

    public override void RemoveChild(IUIElement child)
    {
        base.RemoveChild(child);
        PerformLayout();
    }

    protected override void OnRender(IRenderer renderer)
    {
        var theme = Theme ?? GetInheritedTheme();
        var state = GetCurrentState();

        // Draw background
        var bgColor = BackgroundColor ?? theme?.GetColor(ThemeColor.WindowBackground, state) ?? new DuskColor(200, 200, 200);
        var borderCol = BorderColor ?? theme?.GetColor(ThemeColor.Border, state) ?? DuskColor.Black;

        if (_showBorder)
        {
            renderer.DrawRectangleBeveled(Bounds, bgColor, BevelStyle.Etched, 1);
        }
        else
        {
            renderer.DrawRectangle(Bounds, bgColor, filled: true);
        }

        // Draw title
        if (_showTitle && !string.IsNullOrEmpty(_title))
        {
            var titleFont = theme?.GetFont(ThemeFontRole.Heading) ?? DuskFont.DefaultBold;
            var textColor = theme?.GetColor(ThemeColor.Foreground, state) ?? DuskColor.Black;

            var titleSize = renderer.MeasureText(_title, titleFont);
            var titleX = Bounds.X + PaddingLeft;
            var titleY = Bounds.Y + 2;

            // Draw title background to cover border
            renderer.DrawRectangle(
                new DuskRect(titleX - 2, titleY, titleSize.Width + 4, titleSize.Height),
                bgColor
            );

            renderer.DrawText(_title, new DuskPoint(titleX, titleY), titleFont, textColor);
        }
    }

    public void PerformLayout()
    {
        if (Children.Count == 0) return;

        var contentX = Bounds.X + PaddingLeft;
        var contentY = Bounds.Y + PaddingTop;
        var contentWidth = Bounds.Width - PaddingLeft - PaddingRight;
        var contentHeight = Bounds.Height - PaddingTop - PaddingBottom;

        // Adjust for title
        if (_showTitle && !string.IsNullOrEmpty(_title))
        {
            contentY += 16;
            contentHeight -= 16;
        }

        switch (_layoutMode)
        {
            case LayoutMode.Vertical:
                LayoutVertical(contentX, contentY, contentWidth, contentHeight);
                break;

            case LayoutMode.Horizontal:
                LayoutHorizontal(contentX, contentY, contentWidth, contentHeight);
                break;

            case LayoutMode.Grid:
                LayoutGrid(contentX, contentY, contentWidth, contentHeight);
                break;

            case LayoutMode.Flow:
                LayoutFlow(contentX, contentY, contentWidth, contentHeight);
                break;

            case LayoutMode.Manual:
            default:
                // No automatic layout
                break;
        }
    }

    private void LayoutVertical(int x, int y, int width, int height)
    {
        var currentY = y;
        foreach (var child in Children)
        {
            child.Bounds = new DuskRect(x, currentY, width, child.Bounds.Height);
            currentY += child.Bounds.Height + Spacing;
        }
    }

    private void LayoutHorizontal(int x, int y, int width, int height)
    {
        var currentX = x;
        foreach (var child in Children)
        {
            child.Bounds = new DuskRect(currentX, y, child.Bounds.Width, height);
            currentX += child.Bounds.Width + Spacing;
        }
    }

    private void LayoutGrid(int x, int y, int width, int height)
    {
        var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(Children.Count)));
        var cellWidth = (width - (columns - 1) * Spacing) / columns;
        var rows = (int)Math.Ceiling((double)Children.Count / columns);
        var cellHeight = (height - (rows - 1) * Spacing) / rows;

        for (int i = 0; i < Children.Count; i++)
        {
            var col = i % columns;
            var row = i / columns;
            var cellX = x + col * (cellWidth + Spacing);
            var cellY = y + row * (cellHeight + Spacing);

            Children[i].Bounds = new DuskRect(cellX, cellY, cellWidth, cellHeight);
        }
    }

    private void LayoutFlow(int x, int y, int width, int height)
    {
        var currentX = x;
        var currentY = y;
        var rowHeight = 0;

        foreach (var child in Children)
        {
            if (currentX + child.Bounds.Width > x + width && currentX > x)
            {
                currentX = x;
                currentY += rowHeight + Spacing;
                rowHeight = 0;
            }

            child.Bounds = new DuskRect(currentX, currentY, child.Bounds.Width, child.Bounds.Height);
            currentX += child.Bounds.Width + Spacing;
            rowHeight = Math.Max(rowHeight, child.Bounds.Height);
        }
    }
}

public enum LayoutMode
{
    Manual,
    Vertical,
    Horizontal,
    Grid,
    Flow
}

namespace DUSK.UI;

using DUSK.Core;

/// <summary>
/// List box control for displaying selectable items.
/// Supports single and multiple selection.
/// </summary>
public class UIListBox : UIElementBase
{
    private readonly List<ListItem> _items = new();
    private readonly HashSet<int> _selectedIndices = new();
    private int _scrollOffset;
    private int _hoveredIndex = -1;

    public IReadOnlyList<ListItem> Items => _items.AsReadOnly();
    public IReadOnlySet<int> SelectedIndices => _selectedIndices;
    public int SelectedIndex => _selectedIndices.Count > 0 ? _selectedIndices.First() : -1;

    public ListItem? SelectedItem => SelectedIndex >= 0 && SelectedIndex < _items.Count
        ? _items[SelectedIndex]
        : null;

    public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;
    public int ItemHeight { get; set; } = 20;
    public int ScrollbarWidth { get; set; } = 16;

    public event EventHandler<ListSelectionEventArgs>? SelectionChanged;
    public event EventHandler<ListItemEventArgs>? ItemDoubleClicked;

    public UIListBox(string? id = null) : base(id)
    {
        Bounds = new DuskRect(0, 0, 200, 150);
    }

    public void AddItem(string text, object? tag = null)
    {
        _items.Add(new ListItem(text, tag));
    }

    public void AddItems(IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            AddItem(item);
        }
    }

    public void RemoveItem(int index)
    {
        if (index >= 0 && index < _items.Count)
        {
            _items.RemoveAt(index);
            _selectedIndices.Remove(index);

            // Adjust indices
            var adjusted = _selectedIndices.Where(i => i > index).Select(i => i - 1).ToList();
            _selectedIndices.RemoveWhere(i => i >= index);
            foreach (var i in adjusted) _selectedIndices.Add(i);
        }
    }

    public void ClearItems()
    {
        _items.Clear();
        _selectedIndices.Clear();
        _scrollOffset = 0;
    }

    public void SelectIndex(int index, bool addToSelection = false)
    {
        if (index < 0 || index >= _items.Count) return;

        if (SelectionMode == SelectionMode.Single || !addToSelection)
        {
            _selectedIndices.Clear();
        }

        _selectedIndices.Add(index);
        EnsureVisible(index);
        SelectionChanged?.Invoke(this, new ListSelectionEventArgs(SelectedIndices.ToList()));
    }

    public void ClearSelection()
    {
        _selectedIndices.Clear();
        SelectionChanged?.Invoke(this, new ListSelectionEventArgs(new List<int>()));
    }

    private int VisibleItemCount => (Bounds.Height - 4) / ItemHeight;

    private void EnsureVisible(int index)
    {
        if (index < _scrollOffset)
        {
            _scrollOffset = index;
        }
        else if (index >= _scrollOffset + VisibleItemCount)
        {
            _scrollOffset = index - VisibleItemCount + 1;
        }
    }

    protected override void OnRender(IRenderer renderer)
    {
        var theme = Theme ?? GetInheritedTheme();
        var state = GetCurrentState();

        // Draw background
        var bgColor = theme?.GetColor(ThemeColor.InputBackground, state) ?? DuskColor.White;
        renderer.DrawRectangleBeveled(Bounds, bgColor, BevelStyle.Sunken, 1);

        // Calculate content area
        var contentBounds = new DuskRect(
            Bounds.X + 2,
            Bounds.Y + 2,
            Bounds.Width - 4 - (NeedsScrollbar ? ScrollbarWidth : 0),
            Bounds.Height - 4
        );

        renderer.SetClipRegion(contentBounds);

        // Draw items
        var font = theme?.GetFont(ThemeFontRole.Default) ?? DuskFont.Default;
        var textColor = theme?.GetColor(ThemeColor.Foreground, state) ?? DuskColor.Black;
        var selectedBg = theme?.GetColor(ThemeColor.SelectionBackground, state) ?? DuskColor.AmigaBlue;
        var selectedText = theme?.GetColor(ThemeColor.SelectionText, state) ?? DuskColor.White;
        var hoverBg = theme?.GetColor(ThemeColor.ButtonFace, ElementState.Hover) ?? new DuskColor(230, 230, 230);

        for (int i = _scrollOffset; i < _items.Count && i < _scrollOffset + VisibleItemCount + 1; i++)
        {
            var itemY = contentBounds.Y + (i - _scrollOffset) * ItemHeight;
            var itemRect = new DuskRect(contentBounds.X, itemY, contentBounds.Width, ItemHeight);

            // Draw selection/hover background
            if (_selectedIndices.Contains(i))
            {
                renderer.DrawRectangle(itemRect, selectedBg);
            }
            else if (i == _hoveredIndex)
            {
                renderer.DrawRectangle(itemRect, hoverBg);
            }

            // Draw item text
            var itemTextColor = _selectedIndices.Contains(i) ? selectedText : textColor;
            var textY = itemY + (ItemHeight - renderer.MeasureText(_items[i].Text, font).Height) / 2;
            renderer.DrawText(_items[i].Text, new DuskPoint(contentBounds.X + 4, textY), font, itemTextColor);
        }

        renderer.SetClipRegion(null);

        // Draw scrollbar if needed
        if (NeedsScrollbar)
        {
            DrawScrollbar(renderer, theme);
        }
    }

    private bool NeedsScrollbar => _items.Count > VisibleItemCount;

    private void DrawScrollbar(IRenderer renderer, ITheme? theme)
    {
        var scrollbarRect = new DuskRect(
            Bounds.Right - ScrollbarWidth - 2,
            Bounds.Y + 2,
            ScrollbarWidth,
            Bounds.Height - 4
        );

        var scrollBg = theme?.GetColor(ThemeColor.ButtonFace) ?? DuskColor.AmigaGray;
        renderer.DrawRectangle(scrollbarRect, scrollBg);

        // Calculate thumb position and size
        var thumbHeight = Math.Max(20, scrollbarRect.Height * VisibleItemCount / _items.Count);
        var scrollRange = scrollbarRect.Height - thumbHeight;
        var thumbY = scrollbarRect.Y + (_items.Count > VisibleItemCount
            ? scrollRange * _scrollOffset / (_items.Count - VisibleItemCount)
            : 0);

        var thumbRect = new DuskRect(scrollbarRect.X + 2, (int)thumbY, ScrollbarWidth - 4, (int)thumbHeight);
        var thumbColor = theme?.GetColor(ThemeColor.Primary) ?? DuskColor.AmigaBlue;
        renderer.DrawRectangleBeveled(thumbRect, thumbColor, BevelStyle.Raised, 1);
    }

    public override void HandleMouseDown(MouseEventArgs args)
    {
        base.HandleMouseDown(args);

        var index = GetItemIndexAtPoint(args.Position);
        if (index >= 0)
        {
            var addToSelection = SelectionMode == SelectionMode.Multiple;
            SelectIndex(index, addToSelection);

            if (args.Clicks == 2)
            {
                ItemDoubleClicked?.Invoke(this, new ListItemEventArgs(index, _items[index]));
            }
        }
    }

    private int GetItemIndexAtPoint(DuskPoint point)
    {
        var contentY = Bounds.Y + 2;
        var relativeY = point.Y - contentY;
        var index = _scrollOffset + relativeY / ItemHeight;

        if (index >= 0 && index < _items.Count)
        {
            return index;
        }
        return -1;
    }
}

public record ListItem(string Text, object? Tag = null);

public enum SelectionMode
{
    Single,
    Multiple
}

public class ListSelectionEventArgs : EventArgs
{
    public IReadOnlyList<int> SelectedIndices { get; }

    public ListSelectionEventArgs(IReadOnlyList<int> indices)
    {
        SelectedIndices = indices;
    }
}

public class ListItemEventArgs : EventArgs
{
    public int Index { get; }
    public ListItem Item { get; }

    public ListItemEventArgs(int index, ListItem item)
    {
        Index = index;
        Item = item;
    }
}

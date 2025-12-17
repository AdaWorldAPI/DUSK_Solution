namespace DUSK.UI;

using DUSK.Core;

/// <summary>
/// Combo box (dropdown) control.
/// Combines text display with dropdown selection.
/// </summary>
public class UIComboBox : UIElementBase
{
    private readonly List<ComboItem> _items = new();
    private int _selectedIndex = -1;
    private bool _isDropdownOpen;
    private int _hoveredIndex = -1;
    private int _maxDropdownItems = 8;

    public IReadOnlyList<ComboItem> Items => _items.AsReadOnly();

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < -1 || value >= _items.Count) return;
            if (_selectedIndex == value) return;
            _selectedIndex = value;
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ComboItem? SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count
        ? _items[_selectedIndex]
        : null;

    public string Text => SelectedItem?.Text ?? string.Empty;
    public int DropdownButtonWidth { get; set; } = 20;
    public int MaxDropdownItems
    {
        get => _maxDropdownItems;
        set => _maxDropdownItems = Math.Max(1, value);
    }
    public int ItemHeight { get; set; } = 20;

    public bool IsDropdownOpen => _isDropdownOpen;

    public event EventHandler? SelectedIndexChanged;
    public event EventHandler? DropdownOpened;
    public event EventHandler? DropdownClosed;

    public UIComboBox(string? id = null) : base(id)
    {
        Bounds = new DuskRect(0, 0, 150, 24);
    }

    public void AddItem(string text, object? tag = null)
    {
        _items.Add(new ComboItem(text, tag));
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
            if (_selectedIndex == index)
            {
                _selectedIndex = -1;
            }
            else if (_selectedIndex > index)
            {
                _selectedIndex--;
            }
        }
    }

    public void ClearItems()
    {
        _items.Clear();
        _selectedIndex = -1;
    }

    public void OpenDropdown()
    {
        if (_items.Count == 0) return;
        _isDropdownOpen = true;
        _hoveredIndex = _selectedIndex;
        DropdownOpened?.Invoke(this, EventArgs.Empty);
    }

    public void CloseDropdown()
    {
        _isDropdownOpen = false;
        DropdownClosed?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnRender(IRenderer renderer)
    {
        var theme = Theme ?? GetInheritedTheme();
        var state = GetCurrentState();

        // Draw main combo box
        var bgColor = theme?.GetColor(ThemeColor.InputBackground, state) ?? DuskColor.White;
        var textColor = theme?.GetColor(ThemeColor.InputText, state) ?? DuskColor.Black;
        var font = theme?.GetFont(ThemeFontRole.Default) ?? DuskFont.Default;

        renderer.DrawRectangleBeveled(Bounds, bgColor, BevelStyle.Sunken, 1);

        // Draw selected text
        var textRect = new DuskRect(Bounds.X + 4, Bounds.Y, Bounds.Width - DropdownButtonWidth - 8, Bounds.Height);
        if (!string.IsNullOrEmpty(Text))
        {
            var textY = Bounds.Y + (Bounds.Height - renderer.MeasureText(Text, font).Height) / 2;
            renderer.SetClipRegion(textRect);
            renderer.DrawText(Text, new DuskPoint(textRect.X, textY), font, textColor);
            renderer.SetClipRegion(null);
        }

        // Draw dropdown button
        var buttonRect = new DuskRect(
            Bounds.Right - DropdownButtonWidth,
            Bounds.Y,
            DropdownButtonWidth,
            Bounds.Height
        );
        var buttonColor = theme?.GetColor(ThemeColor.ButtonFace, state) ?? DuskColor.AmigaGray;
        renderer.DrawRectangleBeveled(buttonRect, buttonColor, BevelStyle.Raised, 1);

        // Draw arrow
        var arrowColor = theme?.GetColor(ThemeColor.Foreground, state) ?? DuskColor.Black;
        var arrowX = buttonRect.X + buttonRect.Width / 2;
        var arrowY = buttonRect.Y + buttonRect.Height / 2;
        renderer.DrawText("v", new DuskPoint(arrowX - 3, arrowY - 6), font, arrowColor);

        // Draw dropdown if open
        if (_isDropdownOpen)
        {
            RenderDropdown(renderer, theme, font);
        }
    }

    private void RenderDropdown(IRenderer renderer, ITheme? theme, DuskFont font)
    {
        var displayCount = Math.Min(_items.Count, MaxDropdownItems);
        var dropdownHeight = displayCount * ItemHeight + 4;

        var dropdownRect = new DuskRect(
            Bounds.X,
            Bounds.Bottom,
            Bounds.Width,
            dropdownHeight
        );

        // Draw dropdown background
        var dropdownBg = theme?.GetColor(ThemeColor.WindowBackground) ?? DuskColor.White;
        renderer.DrawRectangleBeveled(dropdownRect, dropdownBg, BevelStyle.Raised, 1);

        // Draw items
        var selectedBg = theme?.GetColor(ThemeColor.SelectionBackground) ?? DuskColor.AmigaBlue;
        var selectedText = theme?.GetColor(ThemeColor.SelectionText) ?? DuskColor.White;
        var normalText = theme?.GetColor(ThemeColor.Foreground) ?? DuskColor.Black;
        var hoverBg = theme?.GetColor(ThemeColor.ButtonFace, ElementState.Hover) ?? new DuskColor(230, 230, 230);

        for (int i = 0; i < displayCount; i++)
        {
            var itemY = dropdownRect.Y + 2 + i * ItemHeight;
            var itemRect = new DuskRect(dropdownRect.X + 2, itemY, dropdownRect.Width - 4, ItemHeight);

            // Draw background
            if (i == _selectedIndex)
            {
                renderer.DrawRectangle(itemRect, selectedBg);
            }
            else if (i == _hoveredIndex)
            {
                renderer.DrawRectangle(itemRect, hoverBg);
            }

            // Draw text
            var itemTextColor = i == _selectedIndex ? selectedText : normalText;
            var textY = itemY + (ItemHeight - renderer.MeasureText(_items[i].Text, font).Height) / 2;
            renderer.DrawText(_items[i].Text, new DuskPoint(itemRect.X + 4, textY), font, itemTextColor);
        }
    }

    public override void HandleMouseDown(MouseEventArgs args)
    {
        base.HandleMouseDown(args);

        if (_isDropdownOpen)
        {
            // Check if clicked on dropdown item
            var dropdownTop = Bounds.Bottom;
            var displayCount = Math.Min(_items.Count, MaxDropdownItems);

            if (args.Position.Y >= dropdownTop)
            {
                var index = (args.Position.Y - dropdownTop - 2) / ItemHeight;
                if (index >= 0 && index < displayCount)
                {
                    SelectedIndex = index;
                }
            }

            CloseDropdown();
        }
        else
        {
            OpenDropdown();
        }
    }

    public override void Blur()
    {
        base.Blur();
        CloseDropdown();
    }
}

public record ComboItem(string Text, object? Tag = null);

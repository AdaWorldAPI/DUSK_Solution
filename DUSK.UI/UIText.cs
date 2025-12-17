namespace DUSK.UI;

using DUSK.Core;

/// <summary>
/// Text display and input control.
/// Can function as a label or editable text field.
/// </summary>
public class UIText : UIElementBase
{
    private string _text = string.Empty;
    private DuskFont _font = DuskFont.Default;
    private int _cursorPosition;
    private int _selectionStart;
    private int _selectionLength;
    private float _cursorBlinkTimer;
    private bool _cursorVisible = true;

    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    public DuskFont Font
    {
        get => _font;
        set => _font = value;
    }

    public bool IsEditable { get; set; }
    public bool IsMultiLine { get; set; }
    public bool IsPassword { get; set; }
    public char PasswordChar { get; set; } = '*';
    public int MaxLength { get; set; } = int.MaxValue;
    public string PlaceholderText { get; set; } = string.Empty;

    public TextAlignment HorizontalAlignment { get; set; } = TextAlignment.Left;
    public TextAlignment VerticalAlignment { get; set; } = TextAlignment.Top;

    public int CursorPosition
    {
        get => _cursorPosition;
        set => _cursorPosition = Math.Clamp(value, 0, _text.Length);
    }

    public event EventHandler<TextChangedEventArgs>? TextChanged;

    public UIText(string? id = null) : base(id)
    {
        PaddingLeft = 4;
        PaddingRight = 4;
        PaddingTop = 2;
        PaddingBottom = 2;
    }

    public UIText(string text, string? id = null) : this(id)
    {
        Text = text;
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (IsFocused && IsEditable)
        {
            _cursorBlinkTimer += deltaTime;
            if (_cursorBlinkTimer >= 0.5f)
            {
                _cursorVisible = !_cursorVisible;
                _cursorBlinkTimer = 0;
            }
        }
    }

    protected override void OnRender(IRenderer renderer)
    {
        var theme = Theme ?? GetInheritedTheme();
        var state = GetCurrentState();

        if (IsEditable)
        {
            RenderEditableText(renderer, theme, state);
        }
        else
        {
            RenderLabel(renderer, theme, state);
        }
    }

    private void RenderLabel(IRenderer renderer, ITheme? theme, ElementState state)
    {
        var textColor = theme?.GetColor(ThemeColor.Foreground, state) ?? DuskColor.Black;
        var displayText = string.IsNullOrEmpty(Text) ? PlaceholderText : Text;
        var displayColor = string.IsNullOrEmpty(Text) ? textColor.WithAlpha(128) : textColor;

        if (string.IsNullOrEmpty(displayText)) return;

        var textSize = renderer.MeasureText(displayText, Font);
        var textPos = CalculateTextPosition(textSize);

        renderer.DrawText(displayText, textPos, Font, displayColor);
    }

    private void RenderEditableText(IRenderer renderer, ITheme? theme, ElementState state)
    {
        // Draw input background
        var bgColor = theme?.GetColor(ThemeColor.InputBackground, state) ?? DuskColor.White;
        var borderColor = theme?.GetColor(ThemeColor.InputBorder, state) ?? DuskColor.Black;
        var textColor = theme?.GetColor(ThemeColor.InputText, state) ?? DuskColor.Black;

        renderer.DrawRectangleBeveled(Bounds, bgColor, BevelStyle.Sunken, 1);

        // Calculate content area
        var contentRect = new DuskRect(
            Bounds.X + PaddingLeft,
            Bounds.Y + PaddingTop,
            Bounds.Width - PaddingLeft - PaddingRight,
            Bounds.Height - PaddingTop - PaddingBottom
        );

        renderer.SetClipRegion(contentRect);

        // Prepare display text
        var displayText = IsPassword
            ? new string(PasswordChar, _text.Length)
            : _text;

        if (string.IsNullOrEmpty(displayText) && !IsFocused)
        {
            displayText = PlaceholderText;
            textColor = textColor.WithAlpha(128);
        }

        // Draw text
        if (!string.IsNullOrEmpty(displayText))
        {
            var textSize = renderer.MeasureText(displayText, Font);
            var textY = contentRect.Y + (contentRect.Height - textSize.Height) / 2;
            renderer.DrawText(displayText, new DuskPoint(contentRect.X, textY), Font, textColor);
        }

        // Draw cursor
        if (IsFocused && _cursorVisible)
        {
            var cursorText = displayText?[.._cursorPosition] ?? string.Empty;
            var cursorX = contentRect.X + renderer.MeasureText(cursorText, Font).Width;
            var cursorY = contentRect.Y + 2;
            var cursorHeight = contentRect.Height - 4;

            renderer.DrawLine(
                new DuskPoint(cursorX, cursorY),
                new DuskPoint(cursorX, cursorY + cursorHeight),
                textColor,
                1
            );
        }

        renderer.SetClipRegion(null);
    }

    private DuskPoint CalculateTextPosition(DuskSize textSize)
    {
        var contentRect = new DuskRect(
            Bounds.X + PaddingLeft,
            Bounds.Y + PaddingTop,
            Bounds.Width - PaddingLeft - PaddingRight,
            Bounds.Height - PaddingTop - PaddingBottom
        );

        var x = HorizontalAlignment switch
        {
            TextAlignment.Left => contentRect.X,
            TextAlignment.Center => contentRect.X + (contentRect.Width - textSize.Width) / 2,
            TextAlignment.Right => contentRect.X + contentRect.Width - textSize.Width,
            _ => contentRect.X
        };

        var y = VerticalAlignment switch
        {
            TextAlignment.Top => contentRect.Y,
            TextAlignment.Center => contentRect.Y + (contentRect.Height - textSize.Height) / 2,
            TextAlignment.Bottom => contentRect.Y + contentRect.Height - textSize.Height,
            _ => contentRect.Y
        };

        return new DuskPoint(x, y);
    }

    public override void HandleKeyDown(KeyEventArgs args)
    {
        base.HandleKeyDown(args);

        if (!IsEditable || !Enabled) return;

        switch (args.Key)
        {
            case DuskKey.Backspace when _cursorPosition > 0:
                _text = _text.Remove(_cursorPosition - 1, 1);
                _cursorPosition--;
                OnTextChanged();
                args.Handled = true;
                break;

            case DuskKey.Delete when _cursorPosition < _text.Length:
                _text = _text.Remove(_cursorPosition, 1);
                OnTextChanged();
                args.Handled = true;
                break;

            case DuskKey.Left when _cursorPosition > 0:
                _cursorPosition--;
                args.Handled = true;
                break;

            case DuskKey.Right when _cursorPosition < _text.Length:
                _cursorPosition++;
                args.Handled = true;
                break;

            case DuskKey.Home:
                _cursorPosition = 0;
                args.Handled = true;
                break;

            case DuskKey.End:
                _cursorPosition = _text.Length;
                args.Handled = true;
                break;
        }
    }

    public void InsertText(string text)
    {
        if (!IsEditable || string.IsNullOrEmpty(text)) return;

        var newLength = _text.Length + text.Length;
        if (newLength > MaxLength)
        {
            text = text[..(MaxLength - _text.Length)];
        }

        _text = _text.Insert(_cursorPosition, text);
        _cursorPosition += text.Length;
        OnTextChanged();
    }

    private void OnTextChanged()
    {
        TextChanged?.Invoke(this, new TextChangedEventArgs(Text));
    }

    public override void Focus()
    {
        base.Focus();
        _cursorVisible = true;
        _cursorBlinkTimer = 0;
    }
}

public enum TextAlignment
{
    Left,
    Center,
    Right,
    Top,
    Bottom
}

public class TextChangedEventArgs : EventArgs
{
    public string Text { get; }

    public TextChangedEventArgs(string text)
    {
        Text = text;
    }
}

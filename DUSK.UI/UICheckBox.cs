namespace DUSK.UI;

using DUSK.Core;

/// <summary>
/// Checkbox control with Amiga MUI style.
/// Supports checked, unchecked, and indeterminate states.
/// </summary>
public class UICheckBox : UIElementBase
{
    private CheckState _checkState = CheckState.Unchecked;
    private string _text = string.Empty;

    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    public bool Checked
    {
        get => _checkState == CheckState.Checked;
        set => CheckState = value ? CheckState.Checked : CheckState.Unchecked;
    }

    public CheckState CheckState
    {
        get => _checkState;
        set
        {
            if (_checkState == value) return;
            _checkState = value;
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool ThreeState { get; set; }
    public int BoxSize { get; set; } = 16;
    public int TextSpacing { get; set; } = 6;

    public event EventHandler? CheckedChanged;

    public UICheckBox(string? id = null) : base(id)
    {
        Bounds = new DuskRect(0, 0, 150, 20);
    }

    public UICheckBox(string text, string? id = null) : this(id)
    {
        Text = text;
    }

    protected override void OnRender(IRenderer renderer)
    {
        var theme = Theme ?? GetInheritedTheme();
        var state = GetCurrentState();

        // Draw checkbox box
        var boxRect = new DuskRect(Bounds.X, Bounds.Y + (Bounds.Height - BoxSize) / 2, BoxSize, BoxSize);
        var boxBg = theme?.GetColor(ThemeColor.InputBackground, state) ?? DuskColor.White;
        var boxBorder = theme?.GetColor(ThemeColor.InputBorder, state) ?? DuskColor.Black;

        renderer.DrawRectangleBeveled(boxRect, boxBg, BevelStyle.Sunken, 1);

        // Draw check mark
        if (_checkState == CheckState.Checked)
        {
            var checkColor = theme?.GetColor(ThemeColor.Primary, state) ?? DuskColor.AmigaBlue;
            var innerRect = boxRect.Inflate(-3, -3);

            // Draw checkmark as two lines
            renderer.DrawLine(
                new DuskPoint(innerRect.X, innerRect.Y + innerRect.Height / 2),
                new DuskPoint(innerRect.X + innerRect.Width / 3, innerRect.Bottom - 2),
                checkColor, 2
            );
            renderer.DrawLine(
                new DuskPoint(innerRect.X + innerRect.Width / 3, innerRect.Bottom - 2),
                new DuskPoint(innerRect.Right, innerRect.Y),
                checkColor, 2
            );
        }
        else if (_checkState == CheckState.Indeterminate)
        {
            var indColor = theme?.GetColor(ThemeColor.Secondary, state) ?? DuskColor.AmigaGray;
            var innerRect = boxRect.Inflate(-4, -4);
            renderer.DrawRectangle(innerRect, indColor);
        }

        // Draw label
        if (!string.IsNullOrEmpty(_text))
        {
            var textColor = theme?.GetColor(ThemeColor.Foreground, state) ?? DuskColor.Black;
            var font = theme?.GetFont(ThemeFontRole.Default) ?? DuskFont.Default;
            var textX = Bounds.X + BoxSize + TextSpacing;
            var textY = Bounds.Y + (Bounds.Height - renderer.MeasureText(_text, font).Height) / 2;

            renderer.DrawText(_text, new DuskPoint(textX, textY), font, textColor);
        }
    }

    public override void HandleMouseUp(MouseEventArgs args)
    {
        base.HandleMouseUp(args);

        if (IsPressed && Enabled)
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (ThreeState)
        {
            CheckState = _checkState switch
            {
                CheckState.Unchecked => CheckState.Checked,
                CheckState.Checked => CheckState.Indeterminate,
                CheckState.Indeterminate => CheckState.Unchecked,
                _ => CheckState.Unchecked
            };
        }
        else
        {
            Checked = !Checked;
        }
    }
}

public enum CheckState
{
    Unchecked,
    Checked,
    Indeterminate
}

namespace DUSK.UI;

using DUSK.Core;

/// <summary>
/// Radio button control for mutually exclusive options.
/// Groups are managed by parent container.
/// </summary>
public class UIRadioButton : UIElementBase
{
    private bool _checked;
    private string _text = string.Empty;
    private string _groupName = "default";

    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;

            if (value)
            {
                // Uncheck siblings in same group
                UncheckSiblings();
            }

            _checked = value;
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string GroupName
    {
        get => _groupName;
        set => _groupName = value ?? "default";
    }

    public int CircleSize { get; set; } = 16;
    public int TextSpacing { get; set; } = 6;

    public event EventHandler? CheckedChanged;

    public UIRadioButton(string? id = null) : base(id)
    {
        Bounds = new DuskRect(0, 0, 150, 20);
    }

    public UIRadioButton(string text, string? id = null) : this(id)
    {
        Text = text;
    }

    protected override void OnRender(IRenderer renderer)
    {
        var theme = Theme ?? GetInheritedTheme();
        var state = GetCurrentState();

        // Draw radio circle (using rectangle as approximation)
        var circleRect = new DuskRect(
            Bounds.X,
            Bounds.Y + (Bounds.Height - CircleSize) / 2,
            CircleSize,
            CircleSize
        );

        var circleBg = theme?.GetColor(ThemeColor.InputBackground, state) ?? DuskColor.White;
        var circleBorder = theme?.GetColor(ThemeColor.InputBorder, state) ?? DuskColor.Black;

        renderer.DrawRectangleBeveled(circleRect, circleBg, BevelStyle.Sunken, 1);

        // Draw filled circle if checked
        if (_checked)
        {
            var checkColor = theme?.GetColor(ThemeColor.Primary, state) ?? DuskColor.AmigaBlue;
            var innerRect = circleRect.Inflate(-4, -4);
            renderer.DrawRectangle(innerRect, checkColor);
        }

        // Draw label
        if (!string.IsNullOrEmpty(_text))
        {
            var textColor = theme?.GetColor(ThemeColor.Foreground, state) ?? DuskColor.Black;
            var font = theme?.GetFont(ThemeFontRole.Default) ?? DuskFont.Default;
            var textX = Bounds.X + CircleSize + TextSpacing;
            var textY = Bounds.Y + (Bounds.Height - renderer.MeasureText(_text, font).Height) / 2;

            renderer.DrawText(_text, new DuskPoint(textX, textY), font, textColor);
        }
    }

    public override void HandleMouseUp(MouseEventArgs args)
    {
        base.HandleMouseUp(args);

        if (IsPressed && Enabled && !_checked)
        {
            Checked = true;
        }
    }

    private void UncheckSiblings()
    {
        if (Parent == null) return;

        foreach (var sibling in Parent.Children)
        {
            if (sibling is UIRadioButton radio &&
                radio != this &&
                radio.GroupName == _groupName &&
                radio.Checked)
            {
                radio._checked = false;
                radio.CheckedChanged?.Invoke(radio, EventArgs.Empty);
            }
        }
    }
}

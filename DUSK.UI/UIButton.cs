namespace DUSK.UI;

using DUSK.Core;

/// <summary>
/// Button control with Amiga MUI style beveled edges.
/// Supports text and optional icon.
/// </summary>
public class UIButton : UIElementBase
{
    private string _text = string.Empty;
    private DuskFont _font = DuskFont.Default;
    private ButtonStyle _style = ButtonStyle.Standard;

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

    public ButtonStyle Style
    {
        get => _style;
        set => _style = value;
    }

    public DuskImage? Icon { get; set; }
    public IconPosition IconPosition { get; set; } = IconPosition.Left;
    public int IconSpacing { get; set; } = 4;

    public UIButton(string? id = null) : base(id)
    {
        PaddingLeft = 8;
        PaddingRight = 8;
        PaddingTop = 4;
        PaddingBottom = 4;
    }

    public UIButton(string text, string? id = null) : this(id)
    {
        Text = text;
    }

    protected override void OnRender(IRenderer renderer)
    {
        var theme = Theme ?? GetInheritedTheme();
        var state = GetCurrentState();

        // Get colors from theme
        var bgColor = theme?.GetColor(ThemeColor.ButtonFace, state) ?? GetDefaultBackgroundColor(state);
        var textColor = theme?.GetColor(ThemeColor.ButtonText, state) ?? DuskColor.Black;
        var bevelStyle = IsPressed ? BevelStyle.Sunken : BevelStyle.Raised;

        if (_style == ButtonStyle.Flat && state == ElementState.Normal)
        {
            bevelStyle = BevelStyle.None;
        }
        else if (_style == ButtonStyle.AmigaMUI)
        {
            bevelStyle = IsPressed ? BevelStyle.SunkenSoft : BevelStyle.AmigaMUI;
        }

        // Draw background with bevel
        var bevelDepth = theme?.GetMetric(ThemeMetric.BevelDepth) ?? 2;
        renderer.DrawRectangleBeveled(Bounds, bgColor, bevelStyle, bevelDepth);

        // Calculate content area
        var contentX = Bounds.X + PaddingLeft;
        var contentY = Bounds.Y + PaddingTop;
        var contentWidth = Bounds.Width - PaddingLeft - PaddingRight;
        var contentHeight = Bounds.Height - PaddingTop - PaddingBottom;

        // Apply pressed offset
        if (IsPressed)
        {
            contentX += 1;
            contentY += 1;
        }

        // Draw icon if present
        var textX = contentX;
        if (Icon != null)
        {
            var iconX = IconPosition == IconPosition.Left ? contentX : contentX + contentWidth - Icon.Width;
            var iconY = contentY + (contentHeight - Icon.Height) / 2;
            renderer.DrawImage(Icon, new DuskRect(iconX, iconY, Icon.Width, Icon.Height));

            if (IconPosition == IconPosition.Left)
            {
                textX += Icon.Width + IconSpacing;
            }
        }

        // Draw text centered
        if (!string.IsNullOrEmpty(Text))
        {
            var textSize = renderer.MeasureText(Text, Font);
            var textY = contentY + (contentHeight - textSize.Height) / 2;

            // Center horizontally if no icon, otherwise align left
            if (Icon == null)
            {
                textX = contentX + (contentWidth - textSize.Width) / 2;
            }

            renderer.DrawText(Text, new DuskPoint(textX, textY), Font, textColor);
        }
    }

    private static DuskColor GetDefaultBackgroundColor(ElementState state) => state switch
    {
        ElementState.Normal => DuskColor.AmigaGray,
        ElementState.Hover => new DuskColor(180, 180, 180),
        ElementState.Pressed => new DuskColor(150, 150, 150),
        ElementState.Focused => new DuskColor(180, 180, 200),
        ElementState.Disabled => new DuskColor(200, 200, 200),
        _ => DuskColor.AmigaGray
    };
}

public enum ButtonStyle
{
    Standard,
    Flat,
    AmigaMUI,
    Outlined,
    Ghost
}

public enum IconPosition
{
    Left,
    Right,
    Top,
    Bottom
}

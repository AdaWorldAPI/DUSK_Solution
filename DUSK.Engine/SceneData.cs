namespace DUSK.Engine;

using System.Text.Json.Serialization;
using DUSK.Core;

/// <summary>
/// Data transfer objects for scene serialization.
/// Provides a neutral format for saving/loading scene hierarchies.
/// </summary>

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ButtonElementData), "button")]
[JsonDerivedType(typeof(TextElementData), "text")]
[JsonDerivedType(typeof(FormElementData), "form")]
[JsonDerivedType(typeof(CheckBoxElementData), "checkbox")]
[JsonDerivedType(typeof(RadioButtonElementData), "radiobutton")]
[JsonDerivedType(typeof(ProgressBarElementData), "progressbar")]
[JsonDerivedType(typeof(ListBoxElementData), "listbox")]
[JsonDerivedType(typeof(ComboBoxElementData), "combobox")]
[JsonDerivedType(typeof(GenericElementData), "generic")]
public abstract class ElementData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public RectData Bounds { get; set; } = new();
    public PaddingData Padding { get; set; } = new();
    public PaddingData Margin { get; set; } = new();
    public List<ElementData> Children { get; set; } = new();
    public Dictionary<string, object?> CustomProperties { get; set; } = new();
}

public class SceneData
{
    public string Version { get; set; } = "1.0";
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? SceneType { get; set; }
    public RectData Bounds { get; set; } = new();
    public ColorData BackgroundColor { get; set; } = new();
    public string? ThemeId { get; set; }
    public List<ElementData> Elements { get; set; } = new();
    public Dictionary<string, object?> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

public class RectData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public RectData() { }
    public RectData(int x, int y, int width, int height)
    {
        X = x; Y = y; Width = width; Height = height;
    }

    public static RectData From(DuskRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
    public DuskRect ToRect() => new(X, Y, Width, Height);
}

public class ColorData
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; } = 255;

    public ColorData() { }
    public ColorData(byte r, byte g, byte b, byte a = 255)
    {
        R = r; G = g; B = b; A = a;
    }

    public static ColorData From(DuskColor color) => new(color.R, color.G, color.B, color.A);
    public DuskColor ToColor() => new(R, G, B, A);
}

public class PaddingData
{
    public int Left { get; set; }
    public int Top { get; set; }
    public int Right { get; set; }
    public int Bottom { get; set; }

    public PaddingData() { }
    public PaddingData(int left, int top, int right, int bottom)
    {
        Left = left; Top = top; Right = right; Bottom = bottom;
    }
}

public class FontData
{
    public string Family { get; set; } = "Default";
    public int Size { get; set; } = 12;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
}

// Specific element data types

public class GenericElementData : ElementData
{
    public string ElementType { get; set; } = string.Empty;
}

public class ButtonElementData : ElementData
{
    public string Text { get; set; } = string.Empty;
    public string? IconPath { get; set; }
    public string ButtonStyle { get; set; } = "AmigaMUI";
}

public class TextElementData : ElementData
{
    public string Text { get; set; } = string.Empty;
    public bool IsEditable { get; set; }
    public bool IsPassword { get; set; }
    public bool Multiline { get; set; }
    public int MaxLength { get; set; }
    public string? Placeholder { get; set; }
    public string Alignment { get; set; } = "Left";
}

public class FormElementData : ElementData
{
    public string LayoutMode { get; set; } = "Manual";
    public int Spacing { get; set; } = 4;
    public bool ShowBorder { get; set; }
    public string? GroupTitle { get; set; }
}

public class CheckBoxElementData : ElementData
{
    public string Text { get; set; } = string.Empty;
    public string CheckState { get; set; } = "Unchecked";
    public bool ThreeState { get; set; }
}

public class RadioButtonElementData : ElementData
{
    public string Text { get; set; } = string.Empty;
    public bool Checked { get; set; }
    public string GroupName { get; set; } = "default";
}

public class ProgressBarElementData : ElementData
{
    public float Value { get; set; }
    public float Minimum { get; set; }
    public float Maximum { get; set; } = 100f;
    public string Style { get; set; } = "Continuous";
    public bool ShowText { get; set; } = true;
    public string? CustomText { get; set; }
}

public class ListBoxElementData : ElementData
{
    public List<ListItemData> Items { get; set; } = new();
    public List<int> SelectedIndices { get; set; } = new();
    public string SelectionMode { get; set; } = "Single";
}

public class ComboBoxElementData : ElementData
{
    public List<ListItemData> Items { get; set; } = new();
    public int SelectedIndex { get; set; } = -1;
}

public class ListItemData
{
    public string Text { get; set; } = string.Empty;
    public string? TagJson { get; set; }
}

// Event binding data for scene wiring

public class EventBindingData
{
    public string ElementId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string HandlerMethod { get; set; } = string.Empty;
    public Dictionary<string, object?>? Parameters { get; set; }
}

public class SceneBindingsData
{
    public string SceneId { get; set; } = string.Empty;
    public List<EventBindingData> EventBindings { get; set; } = new();
    public Dictionary<string, string> PropertyBindings { get; set; } = new();
}

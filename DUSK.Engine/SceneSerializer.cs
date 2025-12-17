namespace DUSK.Engine;

using System.Text.Json;
using System.Text.Json.Serialization;
using DUSK.Core;
using DUSK.UI;

/// <summary>
/// Serializes and deserializes scenes to/from JSON.
/// Supports full round-trip of scene hierarchies.
/// </summary>
public static class SceneSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly Dictionary<Type, Func<IUIElement, ElementData>> ElementToDataMap = new()
    {
        [typeof(UIButton)] = e => SerializeButton((UIButton)e),
        [typeof(UIText)] = e => SerializeText((UIText)e),
        [typeof(UIForm)] = e => SerializeForm((UIForm)e),
        [typeof(UICheckBox)] = e => SerializeCheckBox((UICheckBox)e),
        [typeof(UIRadioButton)] = e => SerializeRadioButton((UIRadioButton)e),
        [typeof(UIProgressBar)] = e => SerializeProgressBar((UIProgressBar)e),
        [typeof(UIListBox)] = e => SerializeListBox((UIListBox)e),
        [typeof(UIComboBox)] = e => SerializeComboBox((UIComboBox)e),
    };

    /// <summary>
    /// Serialize a scene to JSON string.
    /// </summary>
    public static string Serialize(IScene scene, JsonSerializerOptions? options = null)
    {
        var data = ToSceneData(scene);
        return JsonSerializer.Serialize(data, options ?? DefaultOptions);
    }

    /// <summary>
    /// Serialize a scene to a file.
    /// </summary>
    public static async Task SerializeToFileAsync(IScene scene, string filePath, JsonSerializerOptions? options = null)
    {
        var json = Serialize(scene, options);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Deserialize a scene from JSON string.
    /// Returns SceneData for flexible reconstruction.
    /// </summary>
    public static SceneData? Deserialize(string json, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<SceneData>(json, options ?? DefaultOptions);
    }

    /// <summary>
    /// Deserialize a scene from a file.
    /// </summary>
    public static async Task<SceneData?> DeserializeFromFileAsync(string filePath, JsonSerializerOptions? options = null)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return Deserialize(json, options);
    }

    /// <summary>
    /// Convert a scene to its data representation.
    /// </summary>
    public static SceneData ToSceneData(IScene scene)
    {
        var data = new SceneData
        {
            Id = scene.Id,
            Title = scene.Title,
            SceneType = scene.GetType().FullName,
            Bounds = RectData.From(scene.Bounds),
            BackgroundColor = scene is SceneBase sb
                ? ColorData.From(sb.BackgroundColor)
                : new ColorData(170, 170, 170),
            ThemeId = scene.Theme?.GetType().Name,
            ModifiedAt = DateTime.UtcNow
        };

        foreach (var element in scene.Elements)
        {
            data.Elements.Add(SerializeElement(element));
        }

        return data;
    }

    /// <summary>
    /// Reconstruct a dynamic scene from data.
    /// </summary>
    public static DynamicScene FromSceneData(SceneData data)
    {
        var scene = new DynamicScene(data.Id, data.Title)
        {
            Bounds = data.Bounds.ToRect(),
            BackgroundColor = data.BackgroundColor.ToColor()
        };

        foreach (var elementData in data.Elements)
        {
            var element = DeserializeElement(elementData);
            if (element != null)
            {
                scene.AddElement(element);
            }
        }

        return scene;
    }

    private static ElementData SerializeElement(IUIElement element)
    {
        // Try to find specific serializer
        if (ElementToDataMap.TryGetValue(element.GetType(), out var serializer))
        {
            var data = serializer(element);
            PopulateBaseData(data, element);
            return data;
        }

        // Fall back to generic
        var generic = new GenericElementData
        {
            ElementType = element.GetType().FullName ?? element.GetType().Name
        };
        PopulateBaseData(generic, element);
        return generic;
    }

    private static void PopulateBaseData(ElementData data, IUIElement element)
    {
        data.Id = element.Id;
        data.Name = element.Name;
        data.Visible = element.Visible;
        data.Enabled = element.Enabled;
        data.Bounds = RectData.From(element.Bounds);

        if (element is UIElementBase ueb)
        {
            data.Padding = new PaddingData(ueb.PaddingLeft, ueb.PaddingTop, ueb.PaddingRight, ueb.PaddingBottom);
            data.Margin = new PaddingData(ueb.MarginLeft, ueb.MarginTop, ueb.MarginRight, ueb.MarginBottom);
        }

        foreach (var child in element.Children)
        {
            data.Children.Add(SerializeElement(child));
        }
    }

    private static ButtonElementData SerializeButton(UIButton button)
    {
        return new ButtonElementData
        {
            Text = button.Text,
            ButtonStyle = button.Style.ToString()
        };
    }

    private static TextElementData SerializeText(UIText text)
    {
        return new TextElementData
        {
            Text = text.Text,
            IsEditable = text.IsEditable,
            IsPassword = text.IsPassword,
            Multiline = text.Multiline,
            MaxLength = text.MaxLength,
            Placeholder = text.Placeholder,
            Alignment = text.Alignment.ToString()
        };
    }

    private static FormElementData SerializeForm(UIForm form)
    {
        return new FormElementData
        {
            LayoutMode = form.LayoutMode.ToString(),
            Spacing = form.Spacing,
            ShowBorder = form.ShowBorder,
            GroupTitle = form.GroupTitle
        };
    }

    private static CheckBoxElementData SerializeCheckBox(UICheckBox cb)
    {
        return new CheckBoxElementData
        {
            Text = cb.Text,
            CheckState = cb.CheckState.ToString(),
            ThreeState = cb.ThreeState
        };
    }

    private static RadioButtonElementData SerializeRadioButton(UIRadioButton rb)
    {
        return new RadioButtonElementData
        {
            Text = rb.Text,
            Checked = rb.Checked,
            GroupName = rb.GroupName
        };
    }

    private static ProgressBarElementData SerializeProgressBar(UIProgressBar pb)
    {
        return new ProgressBarElementData
        {
            Value = pb.Value,
            Minimum = pb.Minimum,
            Maximum = pb.Maximum,
            Style = pb.Style.ToString(),
            ShowText = pb.ShowText,
            CustomText = pb.CustomText
        };
    }

    private static ListBoxElementData SerializeListBox(UIListBox lb)
    {
        var data = new ListBoxElementData
        {
            SelectionMode = lb.SelectionMode.ToString(),
            SelectedIndices = lb.SelectedIndices.ToList()
        };

        foreach (var item in lb.Items)
        {
            data.Items.Add(new ListItemData
            {
                Text = item.Text,
                TagJson = item.Tag != null ? JsonSerializer.Serialize(item.Tag) : null
            });
        }

        return data;
    }

    private static ComboBoxElementData SerializeComboBox(UIComboBox cb)
    {
        var data = new ComboBoxElementData
        {
            SelectedIndex = cb.SelectedIndex
        };

        foreach (var item in cb.Items)
        {
            data.Items.Add(new ListItemData
            {
                Text = item.Text,
                TagJson = item.Tag != null ? JsonSerializer.Serialize(item.Tag) : null
            });
        }

        return data;
    }

    private static IUIElement? DeserializeElement(ElementData data)
    {
        IUIElement? element = data switch
        {
            ButtonElementData btn => DeserializeButton(btn),
            TextElementData txt => DeserializeText(txt),
            FormElementData form => DeserializeForm(form),
            CheckBoxElementData cb => DeserializeCheckBox(cb),
            RadioButtonElementData rb => DeserializeRadioButton(rb),
            ProgressBarElementData pb => DeserializeProgressBar(pb),
            ListBoxElementData lb => DeserializeListBox(lb),
            ComboBoxElementData cmb => DeserializeComboBox(cmb),
            GenericElementData gen => DeserializeGeneric(gen),
            _ => null
        };

        if (element != null)
        {
            PopulateBaseElement(element, data);
        }

        return element;
    }

    private static void PopulateBaseElement(IUIElement element, ElementData data)
    {
        element.Name = data.Name;
        element.Visible = data.Visible;
        element.Enabled = data.Enabled;
        element.Bounds = data.Bounds.ToRect();

        if (element is UIElementBase ueb)
        {
            ueb.PaddingLeft = data.Padding.Left;
            ueb.PaddingTop = data.Padding.Top;
            ueb.PaddingRight = data.Padding.Right;
            ueb.PaddingBottom = data.Padding.Bottom;
            ueb.MarginLeft = data.Margin.Left;
            ueb.MarginTop = data.Margin.Top;
            ueb.MarginRight = data.Margin.Right;
            ueb.MarginBottom = data.Margin.Bottom;
        }

        foreach (var childData in data.Children)
        {
            var child = DeserializeElement(childData);
            if (child != null)
            {
                element.AddChild(child);
            }
        }
    }

    private static UIButton DeserializeButton(ButtonElementData data)
    {
        var button = new UIButton(data.Id) { Text = data.Text };
        if (Enum.TryParse<ButtonStyle>(data.ButtonStyle, true, out var style))
        {
            button.Style = style;
        }
        return button;
    }

    private static UIText DeserializeText(TextElementData data)
    {
        var text = new UIText(data.Id)
        {
            Text = data.Text,
            IsEditable = data.IsEditable,
            IsPassword = data.IsPassword,
            Multiline = data.Multiline,
            MaxLength = data.MaxLength,
            Placeholder = data.Placeholder
        };

        if (Enum.TryParse<TextAlignment>(data.Alignment, true, out var alignment))
        {
            text.Alignment = alignment;
        }

        return text;
    }

    private static UIForm DeserializeForm(FormElementData data)
    {
        var form = new UIForm(data.Id)
        {
            Spacing = data.Spacing,
            ShowBorder = data.ShowBorder,
            GroupTitle = data.GroupTitle
        };

        if (Enum.TryParse<LayoutMode>(data.LayoutMode, true, out var layout))
        {
            form.LayoutMode = layout;
        }

        return form;
    }

    private static UICheckBox DeserializeCheckBox(CheckBoxElementData data)
    {
        var cb = new UICheckBox(data.Text, data.Id)
        {
            ThreeState = data.ThreeState
        };

        if (Enum.TryParse<CheckState>(data.CheckState, true, out var state))
        {
            cb.CheckState = state;
        }

        return cb;
    }

    private static UIRadioButton DeserializeRadioButton(RadioButtonElementData data)
    {
        return new UIRadioButton(data.Text, data.Id)
        {
            Checked = data.Checked,
            GroupName = data.GroupName
        };
    }

    private static UIProgressBar DeserializeProgressBar(ProgressBarElementData data)
    {
        var pb = new UIProgressBar(data.Id)
        {
            Minimum = data.Minimum,
            Maximum = data.Maximum,
            Value = data.Value,
            ShowText = data.ShowText,
            CustomText = data.CustomText
        };

        if (Enum.TryParse<ProgressBarStyle>(data.Style, true, out var style))
        {
            pb.Style = style;
        }

        return pb;
    }

    private static UIListBox DeserializeListBox(ListBoxElementData data)
    {
        var lb = new UIListBox(data.Id);

        if (Enum.TryParse<SelectionMode>(data.SelectionMode, true, out var mode))
        {
            lb.SelectionMode = mode;
        }

        foreach (var item in data.Items)
        {
            lb.AddItem(item.Text);
        }

        foreach (var index in data.SelectedIndices)
        {
            lb.SelectIndex(index, addToSelection: true);
        }

        return lb;
    }

    private static UIComboBox DeserializeComboBox(ComboBoxElementData data)
    {
        var cb = new UIComboBox(data.Id);

        foreach (var item in data.Items)
        {
            cb.AddItem(item.Text);
        }

        cb.SelectedIndex = data.SelectedIndex;
        return cb;
    }

    private static UIForm DeserializeGeneric(GenericElementData data)
    {
        // Fall back to form as container for unknown types
        return new UIForm(data.Id);
    }
}

/// <summary>
/// A scene that can be fully constructed from data at runtime.
/// </summary>
public class DynamicScene : SceneBase
{
    public DynamicScene(string? id = null, string? title = null) : base(id, title) { }

    protected override void OnInitialize()
    {
        // Dynamic scenes are fully data-driven
    }
}

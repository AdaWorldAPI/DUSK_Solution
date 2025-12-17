namespace DUSK.Migration;

using DUSK.Core;
using DUSK.UI;

/// <summary>
/// Maps WinForms controls to DUSK UI elements.
/// Core of the drop-in replacement functionality.
/// </summary>
public class ControlMapper : IControlAdapterRegistry
{
    private readonly Dictionary<string, IControlAdapter> _adapters = new();
    private readonly Dictionary<Type, IControlAdapter> _typeAdapters = new();

    public ControlMapper()
    {
        RegisterBuiltInAdapters();
    }

    private void RegisterBuiltInAdapters()
    {
        Register(new ButtonAdapter());
        Register(new TextBoxAdapter());
        Register(new LabelAdapter());
        Register(new PanelAdapter());
        Register(new GroupBoxAdapter());
    }

    public void Register<TSource, TTarget>(IControlAdapter adapter)
    {
        _adapters[typeof(TSource).FullName ?? typeof(TSource).Name] = adapter;
        _typeAdapters[typeof(TSource)] = adapter;
    }

    public void Register(IControlAdapter adapter)
    {
        _adapters[adapter.SourceControlType] = adapter;
    }

    public IControlAdapter? GetAdapter(Type sourceType)
    {
        if (_typeAdapters.TryGetValue(sourceType, out var adapter))
            return adapter;

        // Try by full name
        if (_adapters.TryGetValue(sourceType.FullName ?? sourceType.Name, out adapter))
            return adapter;

        // Try by simple name
        if (_adapters.TryGetValue(sourceType.Name, out adapter))
            return adapter;

        // Try base type
        if (sourceType.BaseType != null)
            return GetAdapter(sourceType.BaseType);

        return null;
    }

    public IControlAdapter? GetAdapter(string sourceTypeName)
    {
        return _adapters.GetValueOrDefault(sourceTypeName);
    }

    public IEnumerable<IControlAdapter> GetAllAdapters()
    {
        return _adapters.Values.Distinct();
    }

    public IUIElement AdaptControl(object control)
    {
        var adapter = GetAdapter(control.GetType());
        if (adapter == null)
        {
            throw new InvalidOperationException(
                $"No adapter registered for control type '{control.GetType().Name}'");
        }

        return adapter.Adapt(control);
    }

    public bool CanAdapt(Type controlType)
    {
        return GetAdapter(controlType) != null;
    }
}

public class ButtonAdapter : IControlAdapter
{
    public string SourceControlType => "System.Windows.Forms.Button";
    public string TargetElementType => nameof(UIButton);

    public IUIElement Adapt(object sourceControl)
    {
        var button = new UIButton();

        // Use reflection to get properties without direct WinForms dependency
        var type = sourceControl.GetType();

        var textProp = type.GetProperty("Text");
        if (textProp != null)
            button.Text = textProp.GetValue(sourceControl) as string ?? string.Empty;

        var nameProp = type.GetProperty("Name");
        if (nameProp != null)
            button.Name = nameProp.GetValue(sourceControl) as string ?? string.Empty;

        var bounds = GetBounds(sourceControl);
        button.Bounds = bounds;

        var enabledProp = type.GetProperty("Enabled");
        if (enabledProp != null)
            button.Enabled = (bool)(enabledProp.GetValue(sourceControl) ?? true);

        var visibleProp = type.GetProperty("Visible");
        if (visibleProp != null)
            button.Visible = (bool)(visibleProp.GetValue(sourceControl) ?? true);

        return button;
    }

    public object CreateCompatibilityShim(IUIElement element)
    {
        // Would create a WinForms Button that wraps the DUSK UIButton
        throw new NotImplementedException("Shim creation requires WinForms reference");
    }

    public ControlMappingInfo GetMappingInfo()
    {
        return new ControlMappingInfo(
            SourceControlType,
            TargetElementType,
            new Dictionary<string, string>
            {
                ["Text"] = "Text",
                ["Name"] = "Name",
                ["Enabled"] = "Enabled",
                ["Visible"] = "Visible",
                ["Bounds"] = "Bounds",
                ["FlatStyle"] = "Style"
            },
            new Dictionary<string, string>
            {
                ["Click"] = "Click",
                ["MouseEnter"] = "MouseEnter",
                ["MouseLeave"] = "MouseLeave"
            },
            new[] { "Text", "Enabled", "Click events", "Basic styling" },
            new[] { "FlatAppearance", "Image", "ImageAlign" }
        );
    }

    private static DuskRect GetBounds(object control)
    {
        var type = control.GetType();
        var boundsProp = type.GetProperty("Bounds");
        if (boundsProp?.GetValue(control) is { } bounds)
        {
            var x = (int)(bounds.GetType().GetProperty("X")?.GetValue(bounds) ?? 0);
            var y = (int)(bounds.GetType().GetProperty("Y")?.GetValue(bounds) ?? 0);
            var w = (int)(bounds.GetType().GetProperty("Width")?.GetValue(bounds) ?? 100);
            var h = (int)(bounds.GetType().GetProperty("Height")?.GetValue(bounds) ?? 23);
            return new DuskRect(x, y, w, h);
        }
        return new DuskRect(0, 0, 100, 23);
    }
}

public class TextBoxAdapter : IControlAdapter
{
    public string SourceControlType => "System.Windows.Forms.TextBox";
    public string TargetElementType => nameof(UIText);

    public IUIElement Adapt(object sourceControl)
    {
        var text = new UIText { IsEditable = true };
        var type = sourceControl.GetType();

        var textProp = type.GetProperty("Text");
        if (textProp != null)
            text.Text = textProp.GetValue(sourceControl) as string ?? string.Empty;

        var nameProp = type.GetProperty("Name");
        if (nameProp != null)
            text.Name = nameProp.GetValue(sourceControl) as string ?? string.Empty;

        var passwordProp = type.GetProperty("UseSystemPasswordChar");
        if (passwordProp != null)
            text.IsPassword = (bool)(passwordProp.GetValue(sourceControl) ?? false);

        var maxLengthProp = type.GetProperty("MaxLength");
        if (maxLengthProp != null)
            text.MaxLength = (int)(maxLengthProp.GetValue(sourceControl) ?? int.MaxValue);

        return text;
    }

    public object CreateCompatibilityShim(IUIElement element)
    {
        throw new NotImplementedException();
    }

    public ControlMappingInfo GetMappingInfo()
    {
        return new ControlMappingInfo(
            SourceControlType,
            TargetElementType,
            new Dictionary<string, string>
            {
                ["Text"] = "Text",
                ["UseSystemPasswordChar"] = "IsPassword",
                ["MaxLength"] = "MaxLength"
            },
            new Dictionary<string, string>
            {
                ["TextChanged"] = "TextChanged"
            },
            new[] { "Text input", "Password mode", "MaxLength" },
            new[] { "Multiline", "ScrollBars", "CharacterCasing" }
        );
    }
}

public class LabelAdapter : IControlAdapter
{
    public string SourceControlType => "System.Windows.Forms.Label";
    public string TargetElementType => nameof(UIText);

    public IUIElement Adapt(object sourceControl)
    {
        var text = new UIText { IsEditable = false };
        var type = sourceControl.GetType();

        var textProp = type.GetProperty("Text");
        if (textProp != null)
            text.Text = textProp.GetValue(sourceControl) as string ?? string.Empty;

        var nameProp = type.GetProperty("Name");
        if (nameProp != null)
            text.Name = nameProp.GetValue(sourceControl) as string ?? string.Empty;

        return text;
    }

    public object CreateCompatibilityShim(IUIElement element) => throw new NotImplementedException();

    public ControlMappingInfo GetMappingInfo()
    {
        return new ControlMappingInfo(
            SourceControlType,
            TargetElementType,
            new Dictionary<string, string> { ["Text"] = "Text" },
            new Dictionary<string, string>(),
            new[] { "Text display" },
            new[] { "AutoSize", "Image", "LinkLabel features" }
        );
    }
}

public class PanelAdapter : IControlAdapter
{
    public string SourceControlType => "System.Windows.Forms.Panel";
    public string TargetElementType => nameof(UIForm);

    public IUIElement Adapt(object sourceControl)
    {
        var form = new UIForm { ShowBorder = false, ShowTitle = false };
        var type = sourceControl.GetType();

        var nameProp = type.GetProperty("Name");
        if (nameProp != null)
            form.Name = nameProp.GetValue(sourceControl) as string ?? string.Empty;

        return form;
    }

    public object CreateCompatibilityShim(IUIElement element) => throw new NotImplementedException();

    public ControlMappingInfo GetMappingInfo()
    {
        return new ControlMappingInfo(
            SourceControlType,
            TargetElementType,
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new[] { "Container" },
            new[] { "AutoScroll", "BorderStyle" }
        );
    }
}

public class GroupBoxAdapter : IControlAdapter
{
    public string SourceControlType => "System.Windows.Forms.GroupBox";
    public string TargetElementType => nameof(UIForm);

    public IUIElement Adapt(object sourceControl)
    {
        var form = new UIForm { ShowBorder = true, ShowTitle = true };
        var type = sourceControl.GetType();

        var textProp = type.GetProperty("Text");
        if (textProp != null)
            form.Title = textProp.GetValue(sourceControl) as string ?? string.Empty;

        var nameProp = type.GetProperty("Name");
        if (nameProp != null)
            form.Name = nameProp.GetValue(sourceControl) as string ?? string.Empty;

        return form;
    }

    public object CreateCompatibilityShim(IUIElement element) => throw new NotImplementedException();

    public ControlMappingInfo GetMappingInfo()
    {
        return new ControlMappingInfo(
            SourceControlType,
            TargetElementType,
            new Dictionary<string, string> { ["Text"] = "Title" },
            new Dictionary<string, string>(),
            new[] { "Container with title" },
            new[] { "FlatStyle" }
        );
    }
}

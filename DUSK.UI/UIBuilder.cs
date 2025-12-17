namespace DUSK.UI;

using DUSK.Core;
using DUSK.Engine;

/// <summary>
/// Fluent builder for constructing UI hierarchies.
/// Enables declarative, AI-friendly UI construction.
/// </summary>
public class UIBuilder
{
    private readonly Stack<IUIElement> _elementStack = new();
    private readonly List<IUIElement> _rootElements = new();
    private IUIElement? _currentElement;
    private ITheme? _theme;

    public UIBuilder WithTheme(ITheme theme)
    {
        _theme = theme;
        return this;
    }

    public UIBuilder Button(string text, Action<UIButton>? configure = null)
    {
        var button = new UIButton(text) { Theme = _theme };
        configure?.Invoke(button);
        AddElement(button);
        return this;
    }

    public UIBuilder Button(string text, Action onClick, Action<UIButton>? configure = null)
    {
        var button = new UIButton(text) { Theme = _theme };
        button.Click += (_, _) => onClick();
        configure?.Invoke(button);
        AddElement(button);
        return this;
    }

    public UIBuilder Text(string text, Action<UIText>? configure = null)
    {
        var textElement = new UIText(text) { Theme = _theme };
        configure?.Invoke(textElement);
        AddElement(textElement);
        return this;
    }

    public UIBuilder Label(string text, Action<UIText>? configure = null)
    {
        var label = new UIText(text) { Theme = _theme, IsEditable = false };
        configure?.Invoke(label);
        AddElement(label);
        return this;
    }

    public UIBuilder TextInput(string placeholder = "", Action<UIText>? configure = null)
    {
        var input = new UIText { Theme = _theme, IsEditable = true, PlaceholderText = placeholder };
        configure?.Invoke(input);
        AddElement(input);
        return this;
    }

    public UIBuilder PasswordInput(string placeholder = "", Action<UIText>? configure = null)
    {
        var input = new UIText
        {
            Theme = _theme,
            IsEditable = true,
            IsPassword = true,
            PlaceholderText = placeholder
        };
        configure?.Invoke(input);
        AddElement(input);
        return this;
    }

    public UIBuilder Form(string title = "", Action<UIForm>? configure = null)
    {
        var form = new UIForm(title) { Theme = _theme };
        configure?.Invoke(form);
        AddElement(form);
        return this;
    }

    public UIBuilder BeginForm(string title = "", LayoutMode layout = LayoutMode.Vertical, Action<UIForm>? configure = null)
    {
        var form = new UIForm(title) { Theme = _theme, LayoutMode = layout };
        configure?.Invoke(form);
        AddElement(form);
        _elementStack.Push(form);
        return this;
    }

    public UIBuilder EndForm()
    {
        if (_elementStack.Count > 0 && _elementStack.Peek() is UIForm)
        {
            var form = (UIForm)_elementStack.Pop();
            form.PerformLayout();
        }
        return this;
    }

    public UIBuilder BeginVertical(Action<UIForm>? configure = null)
    {
        return BeginForm("", LayoutMode.Vertical, f =>
        {
            f.ShowBorder = false;
            f.ShowTitle = false;
            configure?.Invoke(f);
        });
    }

    public UIBuilder EndVertical() => EndForm();

    public UIBuilder BeginHorizontal(Action<UIForm>? configure = null)
    {
        return BeginForm("", LayoutMode.Horizontal, f =>
        {
            f.ShowBorder = false;
            f.ShowTitle = false;
            configure?.Invoke(f);
        });
    }

    public UIBuilder EndHorizontal() => EndForm();

    public UIBuilder Space(int pixels = 10)
    {
        var spacer = new UISpacer(pixels) { Theme = _theme };
        AddElement(spacer);
        return this;
    }

    public UIBuilder Custom<T>(Action<T>? configure = null) where T : UIElementBase, new()
    {
        var element = new T { Theme = _theme };
        configure?.Invoke(element);
        AddElement(element);
        return this;
    }

    public UIBuilder WithBounds(int x, int y, int width, int height)
    {
        if (_currentElement != null)
        {
            _currentElement.Bounds = new DuskRect(x, y, width, height);
        }
        return this;
    }

    public UIBuilder WithSize(int width, int height)
    {
        if (_currentElement != null)
        {
            _currentElement.Bounds = _currentElement.Bounds with { Width = width, Height = height };
        }
        return this;
    }

    public UIBuilder WithId(string id)
    {
        if (_currentElement != null)
        {
            _currentElement.Name = id;
        }
        return this;
    }

    public UIBuilder WithTag(object tag)
    {
        if (_currentElement != null)
        {
            _currentElement.Tag = tag;
        }
        return this;
    }

    private void AddElement(IUIElement element)
    {
        _currentElement = element;

        if (_elementStack.Count > 0)
        {
            _elementStack.Peek().AddChild(element);
        }
        else
        {
            _rootElements.Add(element);
        }
    }

    public IReadOnlyList<IUIElement> Build()
    {
        return _rootElements.AsReadOnly();
    }

    public void BuildInto(SceneBase scene)
    {
        foreach (var element in _rootElements)
        {
            scene.AddElement(element);
        }
    }

    public void BuildInto(IUIElement parent)
    {
        foreach (var element in _rootElements)
        {
            parent.AddChild(element);
        }
    }

    public static UIBuilder Create() => new();
}

/// <summary>
/// Invisible spacer element for layout purposes.
/// </summary>
public class UISpacer : UIElementBase
{
    public int Size { get; set; }

    public UISpacer(int size = 10) : base()
    {
        Size = size;
        Bounds = new DuskRect(0, 0, size, size);
    }

    protected override void OnRender(IRenderer renderer)
    {
        // Spacer is invisible
    }
}

namespace DUSK.Migration;

using DUSK.Core;
using DUSK.Engine;
using DUSK.UI;

/// <summary>
/// Converts WinForms Forms to DUSK Scenes.
/// Main entry point for migration.
/// </summary>
public class FormConverter
{
    private readonly ControlMapper _mapper;
    private readonly WinFormsAnalyzer _analyzer;

    public FormConverter()
    {
        _mapper = new ControlMapper();
        _analyzer = new WinFormsAnalyzer();
    }

    public FormConverter(ControlMapper mapper)
    {
        _mapper = mapper;
        _analyzer = new WinFormsAnalyzer();
    }

    public ConvertedScene Convert(object form)
    {
        var formType = form.GetType();
        var analysis = _analyzer.Analyze(formType);

        var scene = new ConvertedScene(formType.Name);

        // Get form properties
        var textProp = formType.GetProperty("Text");
        if (textProp != null)
            scene.Title = textProp.GetValue(form) as string ?? formType.Name;

        var boundsProp = formType.GetProperty("ClientSize");
        if (boundsProp?.GetValue(form) is { } size)
        {
            var w = (int)(size.GetType().GetProperty("Width")?.GetValue(size) ?? 800);
            var h = (int)(size.GetType().GetProperty("Height")?.GetValue(size) ?? 600);
            scene.Bounds = new DuskRect(0, 0, w, h);
        }

        // Convert controls
        var controlsProp = formType.GetProperty("Controls");
        if (controlsProp?.GetValue(form) is System.Collections.IEnumerable controls)
        {
            ConvertControls(controls, scene);
        }

        scene.AnalysisResult = analysis;
        return scene;
    }

    private void ConvertControls(System.Collections.IEnumerable controls, IUIElement parent)
    {
        foreach (var control in controls)
        {
            if (control == null) continue;

            try
            {
                if (_mapper.CanAdapt(control.GetType()))
                {
                    var element = _mapper.AdaptControl(control);
                    parent.AddChild(element);

                    // Recursively convert child controls
                    var childControlsProp = control.GetType().GetProperty("Controls");
                    if (childControlsProp?.GetValue(control) is System.Collections.IEnumerable childControls)
                    {
                        ConvertControls(childControls, element);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log conversion error but continue
                System.Diagnostics.Debug.WriteLine($"Failed to convert control: {ex.Message}");
            }
        }
    }

    public string GenerateSceneCode(ConvertedScene scene)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("using DUSK.Engine;");
        sb.AppendLine("using DUSK.UI;");
        sb.AppendLine("using DUSK.Core;");
        sb.AppendLine();
        sb.AppendLine($"namespace {scene.AnalysisResult?.Namespace ?? "Converted"};");
        sb.AppendLine();
        sb.AppendLine($"public class {scene.Title}Scene : SceneBase");
        sb.AppendLine("{");

        // Generate fields
        foreach (var element in scene.Elements)
        {
            sb.AppendLine($"    private {element.GetType().Name} {element.Name};");
        }

        sb.AppendLine();
        sb.AppendLine($"    public {scene.Title}Scene() : base(\"{scene.Id}\", \"{scene.Title}\")");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate initialization
        sb.AppendLine("    protected override void OnInitialize()");
        sb.AppendLine("    {");

        foreach (var element in scene.Elements)
        {
            sb.AppendLine($"        {element.Name} = new {element.GetType().Name}");
            sb.AppendLine("        {");
            sb.AppendLine($"            Name = \"{element.Name}\",");
            sb.AppendLine($"            Bounds = new DuskRect({element.Bounds.X}, {element.Bounds.Y}, {element.Bounds.Width}, {element.Bounds.Height})");
            sb.AppendLine("        };");
            sb.AppendLine($"        AddElement({element.Name});");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}

/// <summary>
/// Scene created from a converted WinForms Form.
/// </summary>
public class ConvertedScene : SceneBase
{
    public FormAnalysisResult? AnalysisResult { get; set; }
    public Dictionary<string, string> EventMappings { get; } = new();

    public ConvertedScene(string title) : base(null, title)
    {
    }
}

/// <summary>
/// Namespace hijacking shim for seamless WinForms replacement.
/// Place in a project that shadows System.Windows.Forms.
/// </summary>
public static class WinFormsShim
{
    public static string GenerateShimCode()
    {
        return @"
// Auto-generated WinForms compatibility shim
// This allows existing WinForms code to run on DUSK

namespace System.Windows.Forms
{
    using DUSK.Engine;
    using DUSK.UI;
    using DUSK.Core;

    public class Form : SceneBase
    {
        public string Text
        {
            get => Title;
            set => Title = value;
        }

        public ControlCollection Controls { get; }

        public Form() : base()
        {
            Controls = new ControlCollection(this);
        }

        public void Show() => base.Show();
        public void Close() => base.Close();
    }

    public class Control : UIElementBase
    {
        protected Control() : base() { }
    }

    public class Button : UIButton
    {
        public Button() : base() { }
    }

    public class TextBox : UIText
    {
        public TextBox() : base()
        {
            IsEditable = true;
        }
    }

    public class Label : UIText
    {
        public Label() : base()
        {
            IsEditable = false;
        }
    }

    public class Panel : UIForm
    {
        public Panel() : base()
        {
            ShowBorder = false;
            ShowTitle = false;
        }
    }

    public class GroupBox : UIForm
    {
        public GroupBox() : base()
        {
            ShowBorder = true;
            ShowTitle = true;
        }
    }

    public class ControlCollection : System.Collections.Generic.List<Control>
    {
        private readonly Form _owner;

        public ControlCollection(Form owner)
        {
            _owner = owner;
        }

        public new void Add(Control control)
        {
            base.Add(control);
            _owner.AddElement(control);
        }
    }
}";
    }
}

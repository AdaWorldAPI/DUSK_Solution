namespace DUSK.Migration;

using DUSK.Core;

/// <summary>
/// Provides structured hints for AI-assisted refactoring.
/// Metadata that helps AI tools understand the migration context.
/// </summary>
public class AIRefactorHints
{
    public string SourceFramework { get; set; } = "WinForms";
    public string TargetFramework { get; set; } = "DUSK";
    public List<RefactorRule> Rules { get; } = new();
    public List<PatternMapping> Patterns { get; } = new();
    public List<string> Warnings { get; } = new();

    public static AIRefactorHints CreateDefault()
    {
        var hints = new AIRefactorHints();

        // Type mappings
        hints.Rules.Add(new RefactorRule(
            "System.Windows.Forms.Form",
            "DUSK.Engine.SceneBase",
            "Inherit from SceneBase instead of Form"
        ));

        hints.Rules.Add(new RefactorRule(
            "System.Windows.Forms.Button",
            "DUSK.UI.UIButton",
            "Use UIButton with Click event"
        ));

        hints.Rules.Add(new RefactorRule(
            "System.Windows.Forms.TextBox",
            "DUSK.UI.UIText",
            "Use UIText with IsEditable = true"
        ));

        hints.Rules.Add(new RefactorRule(
            "System.Windows.Forms.Label",
            "DUSK.UI.UIText",
            "Use UIText with IsEditable = false"
        ));

        hints.Rules.Add(new RefactorRule(
            "System.Windows.Forms.Panel",
            "DUSK.UI.UIForm",
            "Use UIForm with ShowBorder = false"
        ));

        hints.Rules.Add(new RefactorRule(
            "System.Windows.Forms.GroupBox",
            "DUSK.UI.UIForm",
            "Use UIForm with ShowBorder = true, ShowTitle = true"
        ));

        // Pattern mappings
        hints.Patterns.Add(new PatternMapping(
            "InitializeComponent()",
            "OnInitialize()",
            "Move control initialization to OnInitialize override"
        ));

        hints.Patterns.Add(new PatternMapping(
            "this.Controls.Add(control)",
            "this.AddElement(element)",
            "Use AddElement instead of Controls.Add"
        ));

        hints.Patterns.Add(new PatternMapping(
            "button.Click += handler",
            "button.Click += handler",
            "Event pattern remains similar"
        ));

        hints.Patterns.Add(new PatternMapping(
            "MessageBox.Show()",
            "// TODO: Implement DUSK dialog",
            "MessageBox not yet implemented in DUSK"
        ));

        // Warnings
        hints.Warnings.Add("DataGridView is not yet supported - consider using UIBuilder for tables");
        hints.Warnings.Add("PrintDocument/PrintPreview not supported");
        hints.Warnings.Add("MDI forms require scene stacking pattern");
        hints.Warnings.Add("Drag and drop requires custom implementation");

        return hints;
    }

    public string GeneratePrompt(FormAnalysisResult analysis)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# AI Refactoring Instructions for DUSK Migration");
        sb.AppendLine();
        sb.AppendLine($"## Source: {analysis.FormName}");
        sb.AppendLine($"## Complexity: {analysis.Complexity}");
        sb.AppendLine();

        sb.AppendLine("## Type Mappings");
        foreach (var rule in Rules)
        {
            sb.AppendLine($"- `{rule.SourceType}` → `{rule.TargetType}`: {rule.Description}");
        }
        sb.AppendLine();

        sb.AppendLine("## Pattern Transformations");
        foreach (var pattern in Patterns)
        {
            sb.AppendLine($"- `{pattern.SourcePattern}` → `{pattern.TargetPattern}`");
            sb.AppendLine($"  Note: {pattern.Description}");
        }
        sb.AppendLine();

        if (analysis.PotentialIssues.Count > 0)
        {
            sb.AppendLine("## Issues to Address");
            foreach (var issue in analysis.PotentialIssues)
            {
                sb.AppendLine($"- [{issue.Severity}] {issue.ControlName}: {issue.Description}");
                if (issue.SuggestedFix != null)
                    sb.AppendLine($"  Suggestion: {issue.SuggestedFix}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## Controls to Convert");
        foreach (var control in analysis.Controls)
        {
            sb.AppendLine($"- {control.Name} ({control.TypeName})");
        }
        sb.AppendLine();

        sb.AppendLine("## Event Handlers to Preserve");
        foreach (var handler in analysis.EventHandlers)
        {
            sb.AppendLine($"- {handler.ControlName}.{handler.EventName} → {handler.HandlerMethodName}");
        }

        return sb.ToString();
    }
}

public record RefactorRule(
    string SourceType,
    string TargetType,
    string Description
);

public record PatternMapping(
    string SourcePattern,
    string TargetPattern,
    string Description
);

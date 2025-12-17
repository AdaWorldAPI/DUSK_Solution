namespace DUSK.Migration;

using System.Reflection;
using DUSK.Core;

/// <summary>
/// Analyzes WinForms applications for migration to DUSK.
/// Identifies forms, controls, event handlers, and potential issues.
/// </summary>
public class WinFormsAnalyzer : IFormAnalyzer
{
    public FormAnalysisResult Analyze(Type formType)
    {
        var controls = new List<ControlInfo>();
        var eventHandlers = new List<EventHandlerInfo>();
        var bindings = new List<PropertyBindingInfo>();
        var issues = new List<MigrationIssue>();

        // Analyze fields for controls
        var fields = formType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (var field in fields)
        {
            if (IsControlType(field.FieldType))
            {
                var controlInfo = AnalyzeControlField(field);
                controls.Add(controlInfo);

                // Check for unsupported controls
                var issue = CheckForIssues(field.FieldType, field.Name);
                if (issue != null) issues.Add(issue);
            }
        }

        // Analyze methods for event handlers
        var methods = formType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (var method in methods)
        {
            if (IsEventHandler(method))
            {
                var handlerInfo = AnalyzeEventHandler(method, controls);
                eventHandlers.Add(handlerInfo);
            }
        }

        var complexity = DetermineComplexity(controls.Count, eventHandlers.Count, issues.Count);

        return new FormAnalysisResult(
            formType.Name,
            formType.Namespace ?? string.Empty,
            controls,
            eventHandlers,
            bindings,
            issues,
            complexity
        );
    }

    public FormAnalysisResult AnalyzeAssembly(string assemblyPath)
    {
        var assembly = Assembly.LoadFrom(assemblyPath);
        var formTypes = assembly.GetTypes()
            .Where(t => IsFormType(t))
            .ToList();

        var allControls = new List<ControlInfo>();
        var allHandlers = new List<EventHandlerInfo>();
        var allBindings = new List<PropertyBindingInfo>();
        var allIssues = new List<MigrationIssue>();

        foreach (var formType in formTypes)
        {
            var result = Analyze(formType);
            allControls.AddRange(result.Controls);
            allHandlers.AddRange(result.EventHandlers);
            allBindings.AddRange(result.PropertyBindings);
            allIssues.AddRange(result.PotentialIssues);
        }

        var complexity = DetermineComplexity(
            allControls.Count,
            allHandlers.Count,
            allIssues.Count
        );

        return new FormAnalysisResult(
            Path.GetFileNameWithoutExtension(assemblyPath),
            "Multiple",
            allControls,
            allHandlers,
            allBindings,
            allIssues,
            complexity
        );
    }

    private static bool IsControlType(Type type)
    {
        var controlTypes = new[]
        {
            "System.Windows.Forms.Control",
            "System.Windows.Forms.Button",
            "System.Windows.Forms.TextBox",
            "System.Windows.Forms.Label",
            "System.Windows.Forms.Panel",
            "System.Windows.Forms.GroupBox",
            "System.Windows.Forms.ComboBox",
            "System.Windows.Forms.ListBox",
            "System.Windows.Forms.CheckBox",
            "System.Windows.Forms.RadioButton"
        };

        return controlTypes.Any(ct =>
            type.FullName == ct ||
            type.BaseType?.FullName == ct ||
            type.GetInterfaces().Any(i => i.FullName == ct));
    }

    private static bool IsFormType(Type type)
    {
        var current = type;
        while (current != null)
        {
            if (current.FullName == "System.Windows.Forms.Form")
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static bool IsEventHandler(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return parameters.Length == 2 &&
               parameters[0].ParameterType == typeof(object) &&
               parameters[1].ParameterType.Name.EndsWith("EventArgs");
    }

    private static ControlInfo AnalyzeControlField(FieldInfo field)
    {
        var properties = new Dictionary<string, object?>();

        // Extract common properties if we can get default values
        var typeName = field.FieldType.Name;

        return new ControlInfo(
            field.Name,
            typeName,
            null, // Parent determined at runtime
            properties,
            Array.Empty<string>()
        );
    }

    private static EventHandlerInfo AnalyzeEventHandler(MethodInfo method, List<ControlInfo> controls)
    {
        // Try to determine which control this handler is for
        var controlName = ExtractControlNameFromHandler(method.Name);
        var eventName = ExtractEventNameFromHandler(method.Name);

        return new EventHandlerInfo(
            controlName,
            eventName,
            method.Name,
            Array.Empty<string>() // Would need IL analysis for references
        );
    }

    private static string ExtractControlNameFromHandler(string methodName)
    {
        // Common patterns: button1_Click, txtName_TextChanged
        var underscoreIndex = methodName.IndexOf('_');
        if (underscoreIndex > 0)
        {
            return methodName[..underscoreIndex];
        }
        return "Unknown";
    }

    private static string ExtractEventNameFromHandler(string methodName)
    {
        var underscoreIndex = methodName.IndexOf('_');
        if (underscoreIndex > 0 && underscoreIndex < methodName.Length - 1)
        {
            return methodName[(underscoreIndex + 1)..];
        }
        return "Unknown";
    }

    private static MigrationIssue? CheckForIssues(Type controlType, string controlName)
    {
        var unsupportedControls = new Dictionary<string, string>
        {
            ["DataGridView"] = "Use DUSK.UI.UIDataGrid (requires implementation)",
            ["WebBrowser"] = "Web browser control not supported",
            ["PrintPreviewControl"] = "Printing not yet implemented",
            ["PropertyGrid"] = "Use custom property editor"
        };

        if (unsupportedControls.TryGetValue(controlType.Name, out var suggestion))
        {
            return new MigrationIssue(
                MigrationIssueSeverity.Warning,
                controlName,
                $"Control type '{controlType.Name}' has limited support",
                suggestion
            );
        }

        return null;
    }

    private static MigrationComplexity DetermineComplexity(int controlCount, int handlerCount, int issueCount)
    {
        var score = controlCount + handlerCount * 2 + issueCount * 5;

        return score switch
        {
            < 10 => MigrationComplexity.Simple,
            < 30 => MigrationComplexity.Moderate,
            < 60 => MigrationComplexity.Complex,
            < 100 => MigrationComplexity.VeryComplex,
            _ => MigrationComplexity.RequiresManualIntervention
        };
    }
}

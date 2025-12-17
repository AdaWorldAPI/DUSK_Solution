namespace DUSK.Core;

/// <summary>
/// Adapter interface for converting WinForms controls to DUSK elements.
/// Used during AI-assisted migration of legacy applications.
/// </summary>
public interface IControlAdapter
{
    string SourceControlType { get; }
    string TargetElementType { get; }

    IUIElement Adapt(object sourceControl);
    object CreateCompatibilityShim(IUIElement element);
    ControlMappingInfo GetMappingInfo();
}

public record ControlMappingInfo(
    string SourceType,
    string TargetType,
    IReadOnlyDictionary<string, string> PropertyMappings,
    IReadOnlyDictionary<string, string> EventMappings,
    string[] SupportedFeatures,
    string[] UnsupportedFeatures
);

/// <summary>
/// Registry for control adapters used during WinForms migration.
/// </summary>
public interface IControlAdapterRegistry
{
    void Register<TSource, TTarget>(IControlAdapter adapter);
    IControlAdapter? GetAdapter(Type sourceType);
    IControlAdapter? GetAdapter(string sourceTypeName);
    IEnumerable<IControlAdapter> GetAllAdapters();

    IUIElement AdaptControl(object control);
    bool CanAdapt(Type controlType);
}

/// <summary>
/// Analyzes WinForms code for migration purposes.
/// </summary>
public interface IFormAnalyzer
{
    FormAnalysisResult Analyze(Type formType);
    FormAnalysisResult AnalyzeAssembly(string assemblyPath);
}

public record FormAnalysisResult(
    string FormName,
    string Namespace,
    IReadOnlyList<ControlInfo> Controls,
    IReadOnlyList<EventHandlerInfo> EventHandlers,
    IReadOnlyList<PropertyBindingInfo> PropertyBindings,
    IReadOnlyList<MigrationIssue> PotentialIssues,
    MigrationComplexity Complexity
);

public record ControlInfo(
    string Name,
    string TypeName,
    string? ParentName,
    IDictionary<string, object?> Properties,
    string[] EventSubscriptions
);

public record EventHandlerInfo(
    string ControlName,
    string EventName,
    string HandlerMethodName,
    string[] ReferencedControls
);

public record PropertyBindingInfo(
    string ControlName,
    string PropertyName,
    string DataSource,
    string DataMember
);

public record MigrationIssue(
    MigrationIssueSeverity Severity,
    string ControlName,
    string Description,
    string? SuggestedFix
);

public enum MigrationIssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public enum MigrationComplexity
{
    Simple,
    Moderate,
    Complex,
    VeryComplex,
    RequiresManualIntervention
}

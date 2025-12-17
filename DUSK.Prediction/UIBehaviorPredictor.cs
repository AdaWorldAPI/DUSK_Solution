namespace DUSK.Prediction;

using DUSK.Core;

/// <summary>
/// Predicts user behavior and UI interactions.
/// Can be used to pre-fetch data, pre-render UI, or suggest next actions.
/// </summary>
public class UIBehaviorPredictor
{
    private readonly IPredictionService _service;
    private readonly List<UserAction> _actionHistory = new();
    private const int MaxHistorySize = 100;

    public UIBehaviorPredictor(IPredictionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Record a user action for learning.
    /// </summary>
    public void RecordAction(UserAction action)
    {
        _actionHistory.Add(action);
        if (_actionHistory.Count > MaxHistorySize)
        {
            _actionHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// Predict the next likely UI element the user will interact with.
    /// </summary>
    public async Task<ElementPrediction?> PredictNextInteractionAsync(
        IScene currentScene,
        CancellationToken ct = default)
    {
        if (!_service.IsAvailable) return null;

        var input = new UIContextInput
        {
            CurrentSceneId = currentScene.Id,
            ElementIds = currentScene.Elements.Select(e => e.Id).ToList(),
            RecentActions = _actionHistory.TakeLast(10).ToList(),
            Timestamp = DateTime.UtcNow
        };

        var result = await _service.PredictAsync<UIContextInput, ElementPrediction>(
            input,
            new PredictionOptions { MinConfidence = 0.3f },
            ct
        );

        return result.Success ? result.Output : null;
    }

    /// <summary>
    /// Predict which data keys the user is likely to need next.
    /// Useful for cache warming.
    /// </summary>
    public async Task<IReadOnlyList<string>> PredictDataAccessAsync(
        string currentKey,
        CancellationToken ct = default)
    {
        if (!_service.IsAvailable) return Array.Empty<string>();

        var input = new DataAccessInput
        {
            CurrentKey = currentKey,
            RecentKeys = _actionHistory
                .Where(a => a.ActionType == ActionType.DataAccess)
                .Select(a => a.TargetId)
                .TakeLast(5)
                .ToList()
        };

        var result = await _service.PredictAsync<DataAccessInput, DataAccessPrediction>(input, ct: ct);
        return result.Success ? result.Output?.PredictedKeys ?? Array.Empty<string>() : Array.Empty<string>();
    }

    /// <summary>
    /// Predict user's preferred theme based on time, usage patterns.
    /// </summary>
    public async Task<ThemePrediction?> PredictPreferredThemeAsync(CancellationToken ct = default)
    {
        if (!_service.IsAvailable) return null;

        var input = new ThemeContextInput
        {
            TimeOfDay = DateTime.Now.TimeOfDay,
            DayOfWeek = DateTime.Now.DayOfWeek,
            RecentThemeChanges = _actionHistory
                .Where(a => a.ActionType == ActionType.ThemeChange)
                .TakeLast(5)
                .ToList()
        };

        var result = await _service.PredictAsync<ThemeContextInput, ThemePrediction>(input, ct: ct);
        return result.Success ? result.Output : null;
    }
}

/// <summary>
/// User action types for tracking.
/// </summary>
public enum ActionType
{
    Click,
    KeyPress,
    Navigation,
    DataAccess,
    ThemeChange,
    Scroll,
    Focus,
    Hover
}

/// <summary>
/// Recorded user action.
/// </summary>
public record UserAction
{
    public ActionType ActionType { get; init; }
    public string TargetId { get; init; } = "";
    public string? SceneId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; init; }
}

// Input/Output DTOs for predictions

public class UIContextInput
{
    public string CurrentSceneId { get; set; } = "";
    public List<string> ElementIds { get; set; } = new();
    public List<UserAction> RecentActions { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class ElementPrediction
{
    public string ElementId { get; set; } = "";
    public ActionType PredictedAction { get; set; }
    public float Confidence { get; set; }
    public TimeSpan EstimatedTimeToAction { get; set; }
}

public class DataAccessInput
{
    public string CurrentKey { get; set; } = "";
    public List<string> RecentKeys { get; set; } = new();
}

public class DataAccessPrediction
{
    public List<string> PredictedKeys { get; set; } = new();
    public Dictionary<string, float> KeyProbabilities { get; set; } = new();
}

public class ThemeContextInput
{
    public TimeSpan TimeOfDay { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public List<UserAction> RecentThemeChanges { get; set; } = new();
}

public class ThemePrediction
{
    public string RecommendedThemeId { get; set; } = "";
    public ThemeMood RecommendedMood { get; set; }
    public float Confidence { get; set; }
}

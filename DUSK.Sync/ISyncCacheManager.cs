namespace DUSK.Sync;

using DUSK.Core;

/// <summary>
/// Extended cache manager interface with sync capabilities.
/// Coordinates between UI state and persistent storage through the 3-layer cache.
/// </summary>
public interface ISyncCacheManager : ICacheOrchestrator
{
    bool IsSyncing { get; }
    SyncState CurrentState { get; }

    event EventHandler<SyncEventArgs>? SyncStarted;
    event EventHandler<SyncEventArgs>? SyncCompleted;
    event EventHandler<SyncErrorEventArgs>? SyncError;

    Task StartSyncAsync(CancellationToken ct = default);
    Task StopSyncAsync(CancellationToken ct = default);
    Task ForceSyncAsync(CancellationToken ct = default);

    Task<SyncStatus> GetSyncStatusAsync(CancellationToken ct = default);
}

public enum SyncState
{
    Idle,
    Syncing,
    Error,
    Paused
}

public class SyncEventArgs : EventArgs
{
    public SyncState State { get; }
    public int ItemsSynced { get; }
    public TimeSpan Duration { get; }

    public SyncEventArgs(SyncState state, int itemsSynced = 0, TimeSpan duration = default)
    {
        State = state;
        ItemsSynced = itemsSynced;
        Duration = duration;
    }
}

public class SyncErrorEventArgs : EventArgs
{
    public Exception Exception { get; }
    public string? FailedKey { get; }
    public CacheLayer? FailedLayer { get; }

    public SyncErrorEventArgs(Exception exception, string? failedKey = null, CacheLayer? failedLayer = null)
    {
        Exception = exception;
        FailedKey = failedKey;
        FailedLayer = failedLayer;
    }
}

public record SyncStatus(
    SyncState State,
    DateTime LastSyncTime,
    int PendingItems,
    int TotalItemsCached,
    CacheHealthReport HealthReport
);

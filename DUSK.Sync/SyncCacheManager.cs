namespace DUSK.Sync;

using System.Diagnostics;
using DUSK.Core;

/// <summary>
/// Full sync cache manager implementation.
/// Coordinates UI state synchronization through the 3-layer cache.
/// </summary>
public sealed class SyncCacheManager : ISyncCacheManager, IDisposable
{
    private readonly CacheOrchestrator _orchestrator;
    private readonly SyncCacheManagerOptions _options;
    private readonly Timer _syncTimer;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private SyncState _state = SyncState.Idle;
    private DateTime _lastSyncTime = DateTime.MinValue;
    private int _pendingItems;
    private bool _disposed;

    public bool IsSyncing => _state == SyncState.Syncing;
    public SyncState CurrentState => _state;

    public event EventHandler<SyncEventArgs>? SyncStarted;
    public event EventHandler<SyncEventArgs>? SyncCompleted;
    public event EventHandler<SyncErrorEventArgs>? SyncError;

    public SyncCacheManager(CacheOrchestrator orchestrator, SyncCacheManagerOptions? options = null)
    {
        _orchestrator = orchestrator;
        _options = options ?? new SyncCacheManagerOptions();
        _syncTimer = new Timer(OnSyncTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task StartSyncAsync(CancellationToken ct = default)
    {
        if (_state == SyncState.Syncing) return;

        _state = SyncState.Idle;
        _syncTimer.Change(_options.SyncInterval, _options.SyncInterval);
        await Task.CompletedTask;
    }

    public async Task StopSyncAsync(CancellationToken ct = default)
    {
        _syncTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _state = SyncState.Paused;
        await Task.CompletedTask;
    }

    public async Task ForceSyncAsync(CancellationToken ct = default)
    {
        await PerformSyncAsync(ct);
    }

    public async Task<SyncStatus> GetSyncStatusAsync(CancellationToken ct = default)
    {
        var health = await _orchestrator.GetHealthReportAsync(ct);
        return new SyncStatus(
            _state,
            _lastSyncTime,
            _pendingItems,
            0, // Would need to aggregate from all layers
            health
        );
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        => _orchestrator.GetAsync<T>(key, ct);

    public Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _pendingItems);
        return _orchestrator.SetAsync(key, value, options, ct);
    }

    public Task InvalidateAsync(string key, CancellationToken ct = default)
        => _orchestrator.InvalidateAsync(key, ct);

    public Task InvalidateByTagAsync(string tag, CancellationToken ct = default)
        => _orchestrator.InvalidateByTagAsync(tag, ct);

    public Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions? options = null, CancellationToken ct = default)
        => _orchestrator.GetOrCreateAsync(key, factory, options, ct);

    public Task WarmupAsync(IEnumerable<string> keys, CancellationToken ct = default)
        => _orchestrator.WarmupAsync(keys, ct);

    public Task<CacheHealthReport> GetHealthReportAsync(CancellationToken ct = default)
        => _orchestrator.GetHealthReportAsync(ct);

    private async void OnSyncTimerElapsed(object? state)
    {
        if (_state == SyncState.Syncing) return;
        await PerformSyncAsync(CancellationToken.None);
    }

    private async Task PerformSyncAsync(CancellationToken ct)
    {
        if (!await _syncLock.WaitAsync(0, ct)) return;

        try
        {
            _state = SyncState.Syncing;
            SyncStarted?.Invoke(this, new SyncEventArgs(SyncState.Syncing));

            var sw = Stopwatch.StartNew();

            // Perform sync operations
            var health = await _orchestrator.GetHealthReportAsync(ct);

            if (!health.IsHealthy)
            {
                _state = SyncState.Error;
                SyncError?.Invoke(this, new SyncErrorEventArgs(
                    new InvalidOperationException("Cache health check failed"),
                    null,
                    health.L2Health.IsConnected ? null :
                    health.L3Health.IsConnected ? CacheLayer.L2_Redis : CacheLayer.L3_MongoDB
                ));
                return;
            }

            sw.Stop();
            var itemsSynced = Interlocked.Exchange(ref _pendingItems, 0);
            _lastSyncTime = DateTime.UtcNow;
            _state = SyncState.Idle;

            SyncCompleted?.Invoke(this, new SyncEventArgs(SyncState.Idle, itemsSynced, sw.Elapsed));
        }
        catch (Exception ex)
        {
            _state = SyncState.Error;
            SyncError?.Invoke(this, new SyncErrorEventArgs(ex));
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _syncTimer.Dispose();
        _syncLock.Dispose();
        _orchestrator.Dispose();
    }
}

public record SyncCacheManagerOptions
{
    public TimeSpan SyncInterval { get; init; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; init; } = 3;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
}

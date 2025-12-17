namespace DUSK.Sync;

using DUSK.Core;

/// <summary>
/// Automatic sync loop that coordinates cache synchronization
/// with UI updates for seamless user experience during refactoring.
/// </summary>
public sealed class AutoSyncLoop : IDisposable
{
    private readonly ISyncCacheManager _cacheManager;
    private readonly AutoSyncLoopOptions _options;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<Func<SyncContext, Task>> _syncHandlers = new();

    private Task? _loopTask;
    private bool _isRunning;
    private int _syncCycle;

    public bool IsRunning => _isRunning;
    public int CurrentCycle => _syncCycle;

    public event EventHandler<SyncCycleEventArgs>? CycleStarted;
    public event EventHandler<SyncCycleEventArgs>? CycleCompleted;
    public event EventHandler<Exception>? CycleError;

    public AutoSyncLoop(ISyncCacheManager cacheManager, AutoSyncLoopOptions? options = null)
    {
        _cacheManager = cacheManager;
        _options = options ?? new AutoSyncLoopOptions();
    }

    public void RegisterHandler(Func<SyncContext, Task> handler)
    {
        _syncHandlers.Add(handler);
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _loopTask = RunLoopAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        if (!_isRunning) return;
        _isRunning = false;

        _cts.Cancel();

        if (_loopTask != null)
        {
            try
            {
                await _loopTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _isRunning)
        {
            try
            {
                await Task.Delay(_options.CycleInterval, ct);

                var cycle = Interlocked.Increment(ref _syncCycle);
                var context = new SyncContext(cycle, DateTime.UtcNow, _cacheManager);

                CycleStarted?.Invoke(this, new SyncCycleEventArgs(cycle, SyncCyclePhase.Started));

                // Run all sync handlers
                foreach (var handler in _syncHandlers)
                {
                    if (ct.IsCancellationRequested) break;

                    try
                    {
                        await handler(context);
                    }
                    catch (Exception ex)
                    {
                        CycleError?.Invoke(this, ex);
                        if (!_options.ContinueOnError) throw;
                    }
                }

                // Force cache sync
                await _cacheManager.ForceSyncAsync(ct);

                CycleCompleted?.Invoke(this, new SyncCycleEventArgs(cycle, SyncCyclePhase.Completed));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                CycleError?.Invoke(this, ex);

                if (!_options.ContinueOnError)
                {
                    _isRunning = false;
                    throw;
                }

                // Backoff on error
                await Task.Delay(_options.ErrorBackoff, ct);
            }
        }
    }

    public void Dispose()
    {
        _isRunning = false;
        _cts.Cancel();
        _cts.Dispose();
        _syncHandlers.Clear();
    }
}

public record AutoSyncLoopOptions
{
    public TimeSpan CycleInterval { get; init; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan ErrorBackoff { get; init; } = TimeSpan.FromSeconds(5);
    public bool ContinueOnError { get; init; } = true;
}

public record SyncContext(
    int Cycle,
    DateTime Timestamp,
    ISyncCacheManager CacheManager
);

public class SyncCycleEventArgs : EventArgs
{
    public int Cycle { get; }
    public SyncCyclePhase Phase { get; }

    public SyncCycleEventArgs(int cycle, SyncCyclePhase phase)
    {
        Cycle = cycle;
        Phase = phase;
    }
}

public enum SyncCyclePhase
{
    Started,
    Completed,
    Error
}

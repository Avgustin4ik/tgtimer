using System.Collections.Concurrent;
using timer_web_app.Database;

namespace timer_web_app.TimerService;

public class TimerSchedulerService : ITimerSchedulerService, IHostedService
{
    private ConcurrentDictionary<string, Timer> _timers;

    public void Set(TimeSpan duration, string? userId, Action? callback)
    {
        if (duration <= TimeSpan.Zero || duration == Timeout.InfiniteTimeSpan) 
            throw new ArgumentOutOfRangeException(nameof(duration));
        if (_timers.ContainsKey(userId))
        {
            //TODO update timer or throw error?
            throw new InvalidOperationException("User has already been set");
        }
        var timer = new Timer(state => TimerExpireCallback(callback, state), null, duration, TimeSpan.MaxValue);
        _timers[userId] = timer;
    }
    
    private void TimerExpireCallback(Action? callback, object? state)
    {
        var timer = (Timer)state;
        timer?.DisposeAsync(); 
        callback?.Invoke();
    }
    
    public bool TryRemove(TimerEntity timer)
    {
        return _timers.TryRemove(timer.UserId, out var t);
    }

    public void ClearAll()
    {
        _timers?.Clear();
    }

    public void RestoreAllTasksFromDb()
    {
        throw new NotImplementedException();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timers ??= new ConcurrentDictionary<string, Timer>();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
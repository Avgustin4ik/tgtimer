namespace timer_web_app.Services.Interfaces;

using System.Collections.Concurrent;
using Database;

public interface ITimerSchedulerService
{
    public void Set(TimeSpan duration, string? userId, ITimerExpireHandler expireHandler);
    public bool TryRemove(string userId);
    public Task ClearAll(CancellationToken token);
    public Task RestoreAllTasksFromDb(TimersDbContext dbContext, ITimerExpireHandler expireHandler);
    ConcurrentDictionary<string, Timer> CachedTimers { get; }
}
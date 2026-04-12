namespace timer_web_app.TimerService;

using Database;

public interface ITimerSchedulerService
{
    public void Set(TimeSpan duration, string? userId);
    public bool TryRemove(TimerEntity timer);
    public Task ClearAll(CancellationToken token);
    public void RestoreAllTasksFromDb();
}
namespace timer_web_app.TimerService;

using Database;

public interface ITimerSchedulerService
{
    public void Set(TimeSpan duration, string? userId, Action? callback);
    public bool TryRemove(TimerEntity timer);
    public void ClearAll();
    public void RestoreAllTasksFromDb();
}
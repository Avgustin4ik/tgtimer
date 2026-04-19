using System.Collections.Concurrent;
using timer_web_app.Database;

namespace timer_web_app.TimerService;

public class TimerSchedulerService : ITimerSchedulerService, IHostedService
{
    private ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>();
    private readonly ILogger<TimerSchedulerService> _logger;
    private readonly TimersDbContext _dbContext;

    public TimerSchedulerService(TimersDbContext dbContext, ILogger<TimerSchedulerService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public void Set(TimeSpan duration, string? userId)
    {
        if (duration <= TimeSpan.Zero || duration == Timeout.InfiniteTimeSpan) 
            throw new ArgumentOutOfRangeException(nameof(duration));
        if(userId == null) 
            throw new ArgumentNullException(nameof(userId));
        if(_timers == null) 
            throw new InvalidOperationException("Timer timers have not been initialized");
        if (_timers.ContainsKey(userId))
        {
            //TODO update timer or throw error?
            throw new InvalidOperationException("User has already been set");
        }
        var timer = new Timer(state => TimerExpireCallback(state), null, duration, TimeSpan.MaxValue);
        _timers[userId] = timer;
    }
    
    private void TimerExpireCallback(object? state)
    {
        var timer = (Timer)state;
        timer?.DisposeAsync(); 
        //todo invoke SendMessage to Telegram Bot callback
    }
    
    public bool TryRemove(TimerEntity timer)
    {
        return _timers.TryRemove(timer.UserId, out var t);
    }

    public async Task ClearAll(CancellationToken token)
    {
        if (_timers == null)
        {
            return;
        }
        var taskToDispose = _timers.Select(x => Task.Run(() => x.Value.DisposeAsync(), token));
        await Task.WhenAll(taskToDispose);
        _logger.LogInformation("All timers disposed during cleaning");
    }

    public void RestoreAllTasksFromDb()
    {
        _timers ??= new ConcurrentDictionary<string, Timer>();
        _timers.Clear();
        foreach (var dbTimerRecord in _dbContext.Timers.Where(x => !x.IsFinished))
        {
            Set(dbTimerRecord.EndsAt - dbTimerRecord.CreatedAt, dbTimerRecord.UserId);
        }
    }

    public ConcurrentDictionary<string, Timer> CachedTimers => _timers;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timers ??= new ConcurrentDictionary<string, Timer>();
        _timers.Clear();
        //TODO do i need to restore all data from db?
        RestoreAllTasksFromDb();
        _logger.LogInformation("TimerSchedulerService StartAsync complete");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await ClearAll(cancellationToken);
        _logger.LogInformation("TimerSchedulerService StopAsync complete");
    }
}
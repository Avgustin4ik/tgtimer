namespace timer_web_app.Services;

using System.Collections.Concurrent;
using Database;
using Interfaces;

public class TimerSchedulerService : ITimerSchedulerService, IHostedService
{
    private ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TimerSchedulerService> _logger;

    public TimerSchedulerService(IServiceProvider serviceProvider, ILogger<TimerSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void Set(TimeSpan duration, string? userId, ITimerExpireHandler expireHandler)
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
        var timer = new Timer(state => TimerExpireCallback(state, userId, expireHandler), null, duration, Timeout.InfiniteTimeSpan);
        _timers[userId] = timer;
    }
    
    private void TimerExpireCallback(object? state, string userId, ITimerExpireHandler expireHandler)
    {
        var timer = (Timer)state;
        timer?.Dispose(); 
        TryRemove(userId);
        _ = Task.Run(async () => 
        {
            await expireHandler.Execute(userId);
        });
    }
    
    public bool TryRemove(string userId)
    {
        return _timers.TryRemove(userId, out var t);
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

    public Task RestoreAllTasksFromDb(TimersDbContext dbContext, ITimerExpireHandler expireHandler)
    {
        try
        {
            _timers ??= new ConcurrentDictionary<string, Timer>();
            _timers.Clear();
            foreach (var dbTimerRecord in dbContext.Timers.Where(x => !x.IsFinished))
            {
                Set(dbTimerRecord.EndsAt - dbTimerRecord.CreatedAt, dbTimerRecord.UserId, expireHandler);
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    public ConcurrentDictionary<string, Timer> CachedTimers => _timers;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _timers ??= new ConcurrentDictionary<string, Timer>();
        _timers.Clear();
        //TODO do i need to restore all data from db?
        using (var scope = _serviceProvider.CreateScope())
        {
            var timersDbContext = scope.ServiceProvider.GetRequiredService<TimersDbContext>();
            var expireHandler = scope.ServiceProvider.GetRequiredService<ITimerExpireHandler>();
            await RestoreAllTasksFromDb(timersDbContext, expireHandler);
        }
        _logger.LogInformation("TimerSchedulerService StartAsync complete");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await ClearAll(cancellationToken);
        _logger.LogInformation("TimerSchedulerService StopAsync complete");
    }
}
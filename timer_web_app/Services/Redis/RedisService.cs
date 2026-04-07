using timer_web_app.Database;

namespace timer_web_app.Services;

public class RedisService : IRedisService
{
    private readonly TimersDbContext _postgresDbContext;
    
    public RedisService(TimersDbContext postgresDbContext)
    {
        _postgresDbContext = postgresDbContext;
    }
    private readonly Dictionary<string, Timer> _delayedTimers = new Dictionary<string, Timer>(10);
    public void AddTimerDelayed(string timerId, TimeSpan delay)
    {
        var timer = new Timer(t =>AddTimer(timerId,t), null, delay, Timeout.InfiniteTimeSpan);
        _delayedTimers.Add(timerId, timer);
    }

    public TimerEntity AddTimer(string timerId, object? timerState)
    {
        DisposeTimer(timerState);
        var timerToAdd = _postgresDbContext.Timers.FirstOrDefault(x => x.TimerId == timerId);
        if (timerToAdd == null) throw new NullReferenceException("Timer not found");
        //add to redis
        _delayedTimers.Remove(timerId);
        return timerToAdd;
    }

    private void DisposeTimer(object? state)
    {
        var timer = (Timer)state;
        timer?.DisposeAsync();
    }

    public IResult RemoveTimer(string timerId)
    {
        throw new NotImplementedException();
    }
}
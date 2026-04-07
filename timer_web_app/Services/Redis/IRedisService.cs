using timer_web_app.Database;

namespace timer_web_app.Services;

public interface IRedisService
{
    public void AddTimerDelayed(string timerId);
    public TimerEntity AddTimer(string timerId);
    public IResult RemoveTimer(string timerId);
}
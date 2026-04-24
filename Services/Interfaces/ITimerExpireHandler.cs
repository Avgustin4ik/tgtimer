namespace timer_web_app.Services.Interfaces;

public interface ITimerExpireHandler
{
    Task Execute(string userId);
}
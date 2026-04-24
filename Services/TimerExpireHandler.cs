namespace timer_web_app.Services;

using Database;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public class TimerExpireHandler : ITimerExpireHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TimerSchedulerService> _logger;
    private readonly IDbContextFactory<TimersDbContext> _dbFactory;
    
    public TimerExpireHandler(IServiceProvider serviceProvider, ILogger<TimerSchedulerService> logger, IDbContextFactory<TimersDbContext> dbFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _dbFactory = dbFactory;
    }

    public async Task Execute(string userId)   // ← async Task
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        try
        {
            var timer = await dbContext.Timers
                .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsFinished);

            if (timer == null)
            {
                throw new NullReferenceException("Timer not found");
                _logger.LogWarning("Timer not found or already finished for user {UserId}", userId);
                return;
            }
            timer.IsFinished = true;
            dbContext.Entry(timer).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Timer marked as finished for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing expired timer for user {UserId}", userId);
        }
    }
}
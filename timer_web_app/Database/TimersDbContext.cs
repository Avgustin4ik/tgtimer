using Microsoft.EntityFrameworkCore;

namespace timer_web_app.Database;

public class TimersDbContext : DbContext
{
    public TimersDbContext(DbContextOptions options) : base(options)
    {
        
    }
    public DbSet<TimerEntity> Timers => Set<TimerEntity>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimerEntity>(entity =>
        {
            entity.HasIndex(timer => timer.UserId);
            entity.HasIndex(timer => timer.EndsAt);
            entity.HasIndex(timer => new { timer.UserId, timer.IsFinished });
        });
        base.OnModelCreating(modelBuilder);
    }
}
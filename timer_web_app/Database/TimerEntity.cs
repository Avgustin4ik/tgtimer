using Microsoft.EntityFrameworkCore;

namespace timer_web_app.Database;

[PrimaryKey("UserId")]
public class TimerEntity
{
    
    public string? TimerId { get; set; } = Guid.NewGuid().ToString();
    public string? UserId { get; set; } = string.Empty;
    public DateTime EndsAt { get; set; }                   
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsFinished { get; set; } = false;
}
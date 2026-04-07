using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using timer_web_app;
using timer_web_app.Database;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer(); // Often used for minimal APIs
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1",new OpenApiInfo(){Title = "TgTimer",Version = "v1"})); // Registers the Swagger generator
builder.Services.AddDbContext<TimersDbContext>(option =>
{
    //todo здесь можно имплементировать IOptionsProvider из книжки 
    var connectionString = builder.Configuration.GetConnectionString("Postgres");
    option.UseNpgsql(connectionString, NpgsqlOptionsAction);
    return;

    void NpgsqlOptionsAction(NpgsqlDbContextOptionsBuilder obj)
    {
        obj.EnableRetryOnFailure();
    }
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Timer API v1"));
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TimersDbContext>();
        dbContext.Database.Migrate();
    }
}

async Task<IResult> AddTimerAsync(HttpContext ctx, [FromQuery] TimeSpan duration, TimersDbContext db)
{
    if (duration <= TimeSpan.Zero) return Results.BadRequest("/api/timers/ -- duration must be > 0");

    var userId = GetUserId(ctx) ?? "anonymous";
    var entityEntry = new TimerEntity
    {
        TimerId = Guid.NewGuid().ToString("N"),
        UserId = userId,
        EndsAt = DateTime.UtcNow + duration,
        CreatedAt = DateTime.UtcNow,
        IsFinished = false
    };
    db.Add(entityEntry);
    await db.SaveChangesAsync();
    return Results.Created($"/api/timers/{userId}", entityEntry);
}

app.MapPost("/api/timers", AddTimerAsync)
    .WithName("CreateTimer");

async Task<IResult> DeleteTimer(HttpContext context, [FromQuery] string id, TimersDbContext db)
{
    try
    {
        var entityToDelete = db.Timers.First(e => e.TimerId == id);
        db.Timers.Remove(entityToDelete);
        db.SaveChanges();
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.BadRequest(new IndexOutOfRangeException("No entity with id"));
    }
}

app.MapDelete("/api/timers/", DeleteTimer);

static string? GetUserId(HttpContext ctx)
{
    return ctx.Request.Headers["X-User-Id"].FirstOrDefault()
           ?? ctx.Request.Query["userId"].FirstOrDefault()
           ?? "test-user-" + Guid.NewGuid().ToString("N")[..8];
}

app.MapGet("/api/timers/{userId}", (HttpContext ctx, TimersDbContext db, [FromQuery] string userId) =>
{
    userId = GetUserId(ctx);
    return Results.Ok(db.Timers.Where(x => x.UserId == userId));
});

#if DEBUG
app.MapGet("api/debug/all-timers", (TimersDbContext db) => Results.Ok((object?)db.Timers))
    .WithName("DebugAllTimers");
#endif
app.Run();

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using timer_web_app.Database;
using timer_web_app.Services;
using timer_web_app.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer(); // Often used for minimal APIs
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1",new OpenApiInfo(){Title = "TgTimer",Version = "v1"})); // Registers the Swagger generator
builder.Services.AddDbContextFactory<TimersDbContext>(option =>
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
builder.Services.AddHostedService<TimerSchedulerService>();
builder.Services.AddSingleton<ITimerSchedulerService, TimerSchedulerService>();
builder.Services.AddScoped<ITimerExpireHandler, TimerExpireHandler>();

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

app.MapPost("/api/timers", AddTimerAsync)
    .WithName("CreateTimer");

app.MapDelete("/api/timers/", DeleteTimer);

app.MapGet("/api/timers/{userId}", (HttpContext ctx, TimersDbContext db, [FromQuery] string userId) =>
{
    userId = GetUserId(ctx);
    return Results.Ok(db.Timers.Where(x => x.UserId == userId));
});

#if DEBUG
app.MapGet("api/debug/timers", (TimersDbContext db) => Results.Ok((object?)db.Timers))
    .WithName("DebugAllTimers");
app.MapGet("api/debug/timers/cached", (ITimerSchedulerService scheduler) =>
    {
        var debugList = scheduler.CachedTimers
            .Select(kvp => kvp.Key)
            .ToList();

        return Results.Ok(debugList);
    })
    .WithName("DebugCachedTimers");
app.MapGet("api/timers/markRandomAsFinished", (TimersDbContext db) =>
{
    try
    {
        var firstOrDefault = db.Timers.FirstOrDefault(x => !x.IsFinished);
        if (firstOrDefault is null) return Results.NotFound("Could not find any unfinished timer");
        
        firstOrDefault.IsFinished = true;
        db.SaveChanges();
        return Results.Ok(firstOrDefault);
    }
    catch (Exception exception)
    {
        return Results.Problem(exception.Message); 
    }
}
    ).WithName("MarkRandomAsFinished");

#endif
app.Run();
return;

Task<IResult> DeleteTimer(HttpContext context, [FromQuery] string id, TimersDbContext db)
{
    try
    {
        try
        {
            var entityToDelete = db.Timers.First(e => e.TimerId == id);
            db.Timers.Remove(entityToDelete);
            db.SaveChanges();
            return Task.FromResult(Results.Ok());
        }
        catch (Exception)
        {
            return Task.FromResult(Results.BadRequest(new IndexOutOfRangeException("No entity with id")));
        }
    }
    catch (Exception exception)
    {
        return Task.FromException<IResult>(exception);
    }
}

async Task<IResult> AddTimerAsync(HttpContext ctx,
    [FromQuery] TimeSpan duration,
    TimersDbContext db,
    [FromServices] ITimerSchedulerService timerService,
    [FromServices] ITimerExpireHandler timerExpireHandler)
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
    timerService.Set(duration, userId, timerExpireHandler);
    return Results.Created($"/api/timers/{userId}", entityEntry);
}

static string? GetUserId(HttpContext ctx)
{
    return ctx.Request.Headers["X-User-Id"].FirstOrDefault()
           ?? ctx.Request.Query["userId"].FirstOrDefault()
           ?? "test-user-" + Guid.NewGuid().ToString("N")[..8];
}
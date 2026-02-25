using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer(); // Often used for minimal APIs
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1",new OpenApiInfo(){Title = "TgTimer",Version = "v1"})); // Registers the Swagger generator

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Timer API v1"));
}

var timers = new ConcurrentDictionary<string, TimerInfo>();

app.MapPost("/api/timers", (HttpContext ctx, [FromQuery] TimeSpan duration) =>
    {
        if (duration <= TimeSpan.Zero)
        {
            return Results.BadRequest("/api/timers/ -- duration must be > 0");
        }
        var userId = GetUserId(ctx) ?? "anonymous";

        var timer = new TimerInfo
        {
            TimerId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            EndsAt = DateTime.UtcNow + duration,
            CreatedAt = DateTime.UtcNow
        };
        //todo проверить на то существует ли таймер или нет, т.к. пользователь может создавать только один таймер в данной версии
        //todo либо оставить возможность обновлять таймер по этому же запросу
        timers[userId] = timer;
        return Results.Created($"/api/timers/{userId}", timers[userId]);
    })
    .WithName("CreateTimer");

app.MapDelete("/api/timers/", (HttpContext context, [FromQuery] string id) 
    => timers.TryRemove(id, out var timer) ? Results.Ok((object?)timer) : Results.BadRequest());

static string? GetUserId(HttpContext ctx)
{
    return ctx.Request.Headers["X-User-Id"].FirstOrDefault()
           ?? ctx.Request.Query["userId"].FirstOrDefault()
           ?? "test-user-" + Guid.NewGuid().ToString("N")[..8];
}

app.MapGet("/api/timers", (HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    return userId == null ? Results.BadRequest("no user found") : Results.Ok(timers[userId]);
});
#if DEBUG
app.MapGet("api/debug/all-timers", () => Results.Ok(timers)).WithName("DebugAllTimers");
#endif
app.Run();
internal record TimerInfo
{
    public string TimerId     { get; init; } = null!;
    public string UserId      { get; init; } = null!;
    public DateTime EndsAt    { get; init; }
    public DateTime CreatedAt { get; init; }
}

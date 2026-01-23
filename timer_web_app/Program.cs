using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer(); // Often used for minimal APIs
builder.Services.AddSwaggerGen(); // Registers the Swagger generator

var app = builder.Build();
app.UseSwaggerUI();

var timers = new ConcurrentDictionary<string, TimerInfo>();

app.MapPost("/api/timers", (HttpContext ctx, [FromQuery] TimeSpan request) =>
    {
        var userId = GetUserId(ctx) ?? "anonymous";

        string GetUserId(HttpContext httpContext)
        {
            throw new NotImplementedException();
        }

        var timer = new TimerInfo
        {
            TimerId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            EndsAt = DateTime.UtcNow + request,
            CreatedAt = DateTime.UtcNow
        };
        //todo проверить на то существует ли таймер или нет, т.к. пользователь может создавать только один таймер в данной версии
        timers[timer.TimerId] = timer;

        return Results.Created($"/api/timers/{timer.TimerId}", timer);
    })
    .WithName("CreateTimer");

// app.MapGet("/timer/get", (string uuid) => $"User {uuid} checking timer");
// app.MapDelete("/timer/remove", (string uuid, string timerUuid) => $"User {uuid} removing timer {timerUuid}");

// Модель для создания таймера
app.Run();

public record TimerInfo
{
    internal string TimerId;
    internal string UserId;
    internal DateTime EndsAt;
    internal DateTime CreatedAt;
}

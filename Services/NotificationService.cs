using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Camstep.Data;
using Camstep.Models;

namespace Camstep.Services;

public class NotificationService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public NotificationService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task GenerateThrottledAsync(int userId)
    {
        var key = "notif-gen:" + userId;
        if (_cache.TryGetValue(key, out _)) return;
        _cache.Set(key, true, TimeSpan.FromMinutes(1));
        await GenerateAsync(userId, DateTime.Now);
    }

    public void InvalidateThrottle(int userId) => _cache.Remove("notif-gen:" + userId);

    public async Task<int> GenerateAsync(int userId, DateTime now)
    {
        var settings = await _db.UserPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId)
                       ?? new UserPreference { UserId = userId };
        var today = now.Date;
        var candidates = new List<Notification>();

        if (settings.NotifyTasks)
        {
            var lead = Math.Clamp(settings.TaskLeadDays, 0, 30);
            var tasks = await _db.Tasks.AsNoTracking()
                .Where(t => t.UserId == userId && !t.IsCompleted)
                .ToListAsync();

            foreach (var t in tasks)
            {
                var due = t.DueDate.Date;
                var link = $"/Calendar?year={due.Year}&month={due.Month}&day={due.Day}";

                if (due < today && due >= today.AddDays(-30))
                {
                    candidates.Add(New(userId, "task", "Tarea vencida",
                        $"«{t.Title}» venció el {Format.Date(due)}.", link, $"task:{t.Id}:overdue:{due:yyyyMMdd}"));
                }
                else if (due >= today && due <= today.AddDays(lead))
                {
                    var when = due == today ? "vence hoy" : due == today.AddDays(1) ? "vence mañana" : $"vence el {Format.Date(due)}";
                    candidates.Add(New(userId, "task", due == today ? "Tarea para hoy" : "Tarea próxima a vencer",
                        $"«{t.Title}» {when} · Prioridad {t.Priority}.", link, $"task:{t.Id}:due:{due:yyyyMMdd}"));
                }
            }
        }

        if (settings.NotifyEvents)
        {
            var from = today.AddDays(-1);
            var to = today.AddDays(8);
            var events = await _db.CalendarEvents.AsNoTracking()
                .Where(e => e.UserId == userId && e.StartsAt >= from && e.StartsAt < to)
                .ToListAsync();

            foreach (var e in events)
            {
                var lead = e.ReminderMinutesBefore ?? settings.EventLeadMinutes;
                if (lead <= 0) continue;

                var start = e.AllDay ? e.StartsAt.Date : e.StartsAt;
                var notifyAt = start.AddMinutes(-lead);
                var end = e.EndsAt ?? (e.AllDay ? start.AddDays(1) : start.AddHours(1));
                if (now < notifyAt || now >= end) continue;

                var when = e.AllDay
                    ? (start.Date == today ? "hoy (todo el día)" : start.Date == today.AddDays(1) ? "mañana (todo el día)" : $"el {Format.Date(start)}")
                    : (start.Date == today ? $"hoy a las {Format.Time(start)}" : start.Date == today.AddDays(1) ? $"mañana a las {Format.Time(start)}" : $"el {Format.Date(start)} a las {Format.Time(start)}");

                candidates.Add(New(userId, "event", "Evento próximo",
                    $"«{e.Title}» {when}{(string.IsNullOrWhiteSpace(e.Location) ? "" : " · " + e.Location)}.",
                    $"/Calendar?year={start.Year}&month={start.Month}&day={start.Day}",
                    $"event:{e.Id}:{e.StartsAt:yyyyMMddHHmm}:{lead}"));
            }
        }

        if (settings.NotifyGoals)
        {
            var lead = Math.Clamp(settings.GoalLeadDays, 0, 60);
            var goals = await _db.Goals.AsNoTracking().Where(g => g.UserId == userId).ToListAsync();

            foreach (var g in goals.Where(g => !g.IsCompleted))
            {
                var deadline = g.Deadline.Date;
                if (deadline < today && deadline >= today.AddDays(-30))
                    candidates.Add(New(userId, "goal", "Meta vencida",
                        $"«{g.Title}» pasó su fecha límite ({Format.Date(deadline)}) con {g.Progress}% de avance.",
                        $"/Goals/Details/{g.Id}", $"goal:{g.Id}:overdue:{deadline:yyyyMMdd}"));
                else if (deadline >= today && deadline <= today.AddDays(lead))
                    candidates.Add(New(userId, "goal", "Meta por vencer",
                        $"«{g.Title}»: {g.TimeLeftText(today).ToLower(Format.Es)} y llevas {g.Progress}%.",
                        $"/Goals/Details/{g.Id}", $"goal:{g.Id}:soon:{deadline:yyyyMMdd}"));
            }
        }

        if (settings.NotifyBudget)
        {
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var budgets = await _db.Budgets.AsNoTracking().Where(b => b.UserId == userId).ToListAsync();

            if (budgets.Count > 0)
            {
                var expenses = await _db.Expenses.AsNoTracking()
                    .Where(x => x.UserId == userId && x.Date >= monthStart && x.Date < monthEnd)
                    .ToListAsync();
                var month = Format.MonthYear(monthStart).ToLower(Format.Es);

                foreach (var b in budgets.Where(b => b.Amount > 0))
                {
                    var spent = b.Category == "" ? expenses.Sum(e => e.Amount) : expenses.Where(e => e.Category == b.Category).Sum(e => e.Amount);
                    var pct = (int)Math.Round(spent / b.Amount * 100);
                    var name = b.Category == "" ? "tu presupuesto de " + month : $"el presupuesto de {b.Category}";
                    var keyBase = $"budget:{monthStart:yyyyMM}:{b.Category}";

                    if (pct > 100)
                        candidates.Add(New(userId, "budget", "Presupuesto superado",
                            $"Superaste {name}: {Format.Money(spent)} de {Format.Money(b.Amount)}.", "/Finance", keyBase + ":100"));
                    else if (pct >= 80)
                        candidates.Add(New(userId, "budget", "Presupuesto al " + pct + "%",
                            $"Ya usaste el {pct}% de {name}.", "/Finance", keyBase + ":80"));
                }
            }
        }

        if (today.DayOfWeek == DayOfWeek.Sunday && now.Hour >= 9)
        {
            var monday = Weeks.StartOf(today);
            var reviewed = await _db.WeeklyReviews.AsNoTracking().AnyAsync(r => r.UserId == userId && r.WeekStart == monday);
            if (!reviewed)
                candidates.Add(New(userId, "review", "Domingo de calma 🍃",
                    "Tómate 5 minutos para mirar tu semana y elegir tus 3 pasos para la próxima.",
                    "/Review", $"review:{monday:yyyyMMdd}"));
        }

        if (candidates.Count == 0) return 0;

        var existing = (await _db.Notifications
            .Where(n => n.UserId == userId)
            .Select(n => n.SourceKey)
            .ToListAsync()).ToHashSet();

        var fresh = candidates
            .GroupBy(c => c.SourceKey).Select(g => g.First())
            .Where(c => !existing.Contains(c.SourceKey))
            .ToList();

        if (fresh.Count == 0) return 0;

        _db.Notifications.AddRange(fresh);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            foreach (var n in fresh) _db.Entry(n).State = EntityState.Detached;
            return 0;
        }

        return fresh.Count;
    }

    private static Notification New(int userId, string kind, string title, string message, string link, string key) => new()
    {
        UserId = userId,
        Kind = kind,
        Title = title,
        Message = message.Length > 300 ? message[..300] : message,
        Link = link,
        SourceKey = key,
        CreatedAt = DateTime.Now
    };
}

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(IServiceScopeFactory scopes, ILogger<NotificationBackgroundService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken).ContinueWith(_ => { });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var service = scope.ServiceProvider.GetRequiredService<NotificationService>();

                var userIds = await db.Users.Select(u => u.Id).ToListAsync(stoppingToken);
                var created = 0;
                foreach (var id in userIds)
                    created += await service.GenerateAsync(id, DateTime.Now);

                var limit = DateTime.Now.AddDays(-60);
                await db.Notifications.Where(n => (n.IsRead || n.IsDismissed) && n.CreatedAt < limit).ExecuteDeleteAsync(stoppingToken);

                if (created > 0) _logger.LogInformation("Avisos creados: {Count}", created);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron revisar los avisos.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ContinueWith(_ => { });
        }
    }
}

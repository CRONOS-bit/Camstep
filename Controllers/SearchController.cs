using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class SearchController : AppController
{
    private readonly AppDbContext _db;
    public SearchController(AppDbContext db) => _db = db;

    public static readonly (string Id, string Name)[] Types =
    {
        ("all", "Todo"), ("task", "Tareas"), ("event", "Eventos"), ("expense", "Gastos"),
        ("income", "Ingresos"), ("goal", "Metas"), ("song", "Música")
    };

    public async Task<IActionResult> Index(string? q, string? type, DateTime? from, DateTime? to, string? sort, int page = 1)
    {
        var userId = CurrentUserId;
        var vm = new SearchViewModel
        {
            Q = (q ?? "").Trim(),
            Type = Types.Any(t => t.Id == type) ? type! : "all",
            From = from?.Date,
            To = to?.Date,
            Sort = sort is "date_desc" or "date_asc" ? sort : "relevance",
            Page = Math.Max(1, page)
        };

        if (vm.Q.Length < 2)
        {
            if (vm.Q.Length == 1) ViewBag.Hint = "Escribe al menos 2 letras.";
            return View(vm);
        }
        if (vm.Q.Length > 100) vm.Q = vm.Q[..100];

        var needle = Text.Normalize(vm.Q);
        var words = needle.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        int Score(string title, params string?[] others)
        {
            var t = Text.Normalize(title);
            var o = Text.Normalize(string.Join(" ", others));
            if (!words.All(w => t.Contains(w) || o.Contains(w))) return 0;
            var score = words.Count(w => t.Contains(w)) * 10 + words.Count(w => o.Contains(w)) * 3;
            if (t == needle) score += 20;
            else if (t.StartsWith(needle)) score += 8;
            return score;
        }

        var results = new List<SearchResult>();

        foreach (var t in await _db.Tasks.AsNoTracking().Where(x => x.UserId == userId).ToListAsync())
        {
            var s = Score(t.Title, t.Description, t.Priority);
            if (s > 0) results.Add(new SearchResult
            {
                Kind = "task", KindLabel = "Tarea", Title = t.Title, Score = s, Date = t.DueDate,
                Detail = $"{(t.IsCompleted ? "Completada" : "Pendiente")} · Prioridad {t.Priority}{(string.IsNullOrWhiteSpace(t.Description) ? "" : " · " + t.Description)}",
                Url = Url.Action("Edit", "Tasks", new { id = t.Id, returnUrl = Request.Path + Request.QueryString })!
            });
        }

        foreach (var e in await _db.CalendarEvents.AsNoTracking().Where(x => x.UserId == userId).ToListAsync())
        {
            var s = Score(e.Title, e.Description, e.Location);
            if (s > 0) results.Add(new SearchResult
            {
                Kind = "event", KindLabel = "Evento", Title = e.Title, Score = s, Date = e.StartsAt,
                Detail = (e.AllDay ? "Todo el día" : Format.Time(e.StartsAt)) + (string.IsNullOrWhiteSpace(e.Location) ? "" : " · " + e.Location),
                Url = Url.Action("Index", "Calendar", new { year = e.StartsAt.Year, month = e.StartsAt.Month, day = e.StartsAt.Day })!
            });
        }

        foreach (var x in await _db.Expenses.AsNoTracking().Where(x => x.UserId == userId).ToListAsync())
        {
            var s = Score(x.Description, x.Category);
            if (s > 0) results.Add(new SearchResult
            {
                Kind = "expense", KindLabel = "Gasto", Title = x.Description, Score = s, Date = x.Date,
                Detail = $"{Format.Money(x.Amount)} · {x.Category}",
                Url = Url.Action("Edit", "Expenses", new { id = x.Id, returnUrl = Request.Path + Request.QueryString })!
            });
        }

        foreach (var x in await _db.Incomes.AsNoTracking().Where(x => x.UserId == userId).ToListAsync())
        {
            var s = Score(x.Description, x.Category);
            if (s > 0) results.Add(new SearchResult
            {
                Kind = "income", KindLabel = "Ingreso", Title = x.Description, Score = s, Date = x.Date,
                Detail = $"{Format.Money(x.Amount)} · {x.Category}",
                Url = Url.Action("Edit", "Incomes", new { id = x.Id, returnUrl = Request.Path + Request.QueryString })!
            });
        }

        foreach (var g in await _db.Goals.AsNoTracking().Where(x => x.UserId == userId).ToListAsync())
        {
            var s = Score(g.Title, g.Description, g.Notes, g.Category);
            if (s > 0) results.Add(new SearchResult
            {
                Kind = "goal", KindLabel = "Meta", Title = g.Title, Score = s, Date = g.Deadline,
                Detail = $"{g.Progress}% · {g.Category}",
                Url = Url.Action("Details", "Goals", new { id = g.Id })!
            });
        }

        foreach (var m in await _db.Songs.AsNoTracking().Where(x => x.UserId == userId).ToListAsync())
        {
            var s = Score(m.Title, m.OriginalFileName);
            if (s > 0) results.Add(new SearchResult
            {
                Kind = "song", KindLabel = "Canción", Title = m.Title, Score = s, Date = null,
                Detail = m.OriginalFileName,
                Url = Url.Action("Index", "Music")!
            });
        }

        if (vm.From != null) results = results.Where(r => r.Date?.Date >= vm.From).ToList();
        if (vm.To != null) results = results.Where(r => r.Date?.Date <= vm.To).ToList();

        vm.CountByType = results.GroupBy(r => r.Kind).ToDictionary(g => g.Key, g => g.Count());
        if (vm.Type != "all") results = results.Where(r => r.Kind == vm.Type).ToList();

        results = vm.Sort switch
        {
            "date_desc" => results.OrderByDescending(r => r.Date ?? DateTime.MinValue).ToList(),
            "date_asc" => results.OrderBy(r => r.Date ?? DateTime.MaxValue).ToList(),
            _ => results.OrderByDescending(r => r.Score).ThenByDescending(r => r.Date ?? DateTime.MinValue).ToList()
        };

        vm.Total = results.Count;
        vm.Page = Math.Min(vm.Page, vm.TotalPages);
        vm.Results = results.Skip((vm.Page - 1) * vm.PageSize).Take(vm.PageSize).ToList();

        return View(vm);
    }
}

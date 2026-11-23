using Microsoft.AspNetCore.Mvc;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class PathController : AppController
{
    private readonly PathService _path;

    public PathController(PathService path) => _path = path;

    public async Task<IActionResult> Index(int week = 0)
    {
        var userId = CurrentUserId;
        week = Math.Clamp(week, -52, 0);
        var today = DateTime.Today;
        var monday = Weeks.StartOf(today).AddDays(7 * week);
        var start8 = monday.AddDays(-7 * 7);

        var all = await _path.BetweenAsync(userId, start8, monday.AddDays(7));
        var thisWeek = all.Where(f => f.When >= monday).ToList();
        var lastWeek = all.Count(f => f.When >= monday.AddDays(-7) && f.When < monday);

        var model = new PathViewModel
        {
            Week = week,
            Monday = monday,
            Today = today,
            Footprints = thisWeek,
            LastWeekCount = lastWeek,
            Streak = await _path.StreakAsync(userId, today),
            History = Enumerable.Range(0, 8).Select(i =>
            {
                var m = start8.AddDays(7 * i);
                return (m, all.Count(f => f.When >= m && f.When < m.AddDays(7)));
            }).ToList()
        };
        model.BuildDrawing();
        return View(model);
    }
}

using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;

namespace Camstep.Controllers;

public class GoalsController : AppController
{
    private readonly AppDbContext _db;
    public GoalsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? status, string? category)
    {
        var userId = CurrentUserId;
        var today = DateTime.Today;
        var all = await _db.Goals.Where(x => x.UserId == userId).ToListAsync();
        var steps = await _db.GoalSteps.Where(s => s.UserId == userId).ToListAsync();

        var vm = new GoalListViewModel
        {
            Today = today,
            Status = status is "pending" or "completed" or "overdue" ? status : "all",
            Category = Goal.Categories.Contains(category) ? category! : "",
            TotalCount = all.Count,
            PendingCount = all.Count(g => g.Status(today) == "pending"),
            CompletedCount = all.Count(g => g.IsCompleted),
            OverdueCount = all.Count(g => g.IsOverdue(today))
        };

        IEnumerable<Goal> goals = all;
        if (vm.Status != "all") goals = goals.Where(g => g.Status(today) == vm.Status);
        if (vm.Category != "") goals = goals.Where(g => g.Category == vm.Category);

        vm.Goals = goals.OrderBy(g => g.IsCompleted).ThenBy(g => g.Deadline).ToList();
        vm.Steps = steps.GroupBy(s => s.GoalId)
            .ToDictionary(g => g.Key, g => (g.Count(s => s.IsDone), g.Count()));

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = CurrentUserId;
        var goal = await _db.Goals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (goal == null) return NotFound();

        var steps = await _db.GoalSteps.Where(s => s.GoalId == id && s.UserId == userId).ToListAsync();

        return View(new GoalDetailsViewModel
        {
            Goal = goal,
            Steps = steps.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToList(),
            Today = DateTime.Today
        });
    }

    public IActionResult Create() => View(new Goal());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Goal item)
    {
        Validate(item);
        if (!ModelState.IsValid) return View(item);

        var goal = new Goal { UserId = CurrentUserId };
        Copy(item, goal);
        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();

        TempData["Notice"] = "Meta creada. Puedes dividirla en pasos pequeños.";
        return RedirectToAction(nameof(Details), new { id = goal.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Goals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Goal item)
    {
        Validate(item);
        if (!ModelState.IsValid) return View(item);

        var existing = await _db.Goals.FirstOrDefaultAsync(x => x.Id == item.Id && x.UserId == CurrentUserId);
        if (existing == null) return NotFound();

        Copy(item, existing);
        await _db.SaveChangesAsync();

        TempData["Notice"] = "Meta actualizada.";
        return RedirectToAction(nameof(Details), new { id = existing.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProgress(int id, string? amount, string? returnUrl)
    {
        var goal = await _db.Goals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);
        if (goal == null) return NotFound();

        if (!decimal.TryParse((amount ?? "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value == 0)
        {
            TempData["Error"] = "Escribe una cantidad distinta de 0 (usa un número negativo para restar).";
            return RedirectBack(returnUrl ?? Url.Action(nameof(Details), new { id }), nameof(Index));
        }

        var wasCompleted = goal.IsCompleted;
        goal.Current = Math.Clamp(goal.Current + value, 0, goal.Target);
        await _db.SaveChangesAsync();

        TempData["Notice"] = !wasCompleted && goal.IsCompleted
            ? $"¡Cumpliste la meta «{goal.Title}»! 🎉"
            : $"Progreso actualizado: {goal.Progress}%.";
        return RedirectBack(returnUrl ?? Url.Action(nameof(Details), new { id }), nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId;
        var item = await _db.Goals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (item != null)
        {
            _db.GoalSteps.RemoveRange(_db.GoalSteps.Where(s => s.GoalId == id && s.UserId == userId));
            _db.Goals.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Notice"] = "Meta eliminada.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStep(int id, string? title)
    {
        var userId = CurrentUserId;
        if (!await _db.Goals.AnyAsync(g => g.Id == id && g.UserId == userId)) return NotFound();

        title = (title ?? "").Trim();
        if (title.Length is 0 or > 150)
        {
            TempData["Error"] = "El paso debe tener entre 1 y 150 caracteres.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var count = await _db.GoalSteps.CountAsync(s => s.GoalId == id && s.UserId == userId);
        if (count >= 50)
        {
            TempData["Error"] = "Una meta admite hasta 50 pasos.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _db.GoalSteps.Add(new GoalStep { GoalId = id, UserId = userId, Title = title, SortOrder = count });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStep(int stepId)
    {
        var step = await _db.GoalSteps.FirstOrDefaultAsync(s => s.Id == stepId && s.UserId == CurrentUserId);
        if (step == null) return NotFound();

        step.IsDone = !step.IsDone;
        step.CompletedAt = step.IsDone ? DateTime.Now : null;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = step.GoalId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStep(int stepId)
    {
        var step = await _db.GoalSteps.FirstOrDefaultAsync(s => s.Id == stepId && s.UserId == CurrentUserId);
        if (step == null) return NotFound();

        _db.GoalSteps.Remove(step);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = step.GoalId });
    }

    private void Validate(Goal item)
    {
        if (item.Deadline.Year < 2000 || item.Deadline.Year > 2100)
            ModelState.AddModelError(nameof(item.Deadline), "Elige una fecha entre los años 2000 y 2100.");

        if (!Goal.Categories.Contains(item.Category))
            ModelState.AddModelError(nameof(item.Category), "Elige una categoría de la lista.");
    }

    private static void Copy(Goal from, Goal to)
    {
        to.Title = from.Title.Trim();
        to.Description = (from.Description ?? "").Trim();
        to.Notes = (from.Notes ?? "").Trim();
        to.Category = from.Category;
        to.Target = from.Target;
        to.Current = Math.Clamp(from.Current, 0, from.Target);
        to.Deadline = from.Deadline.Date;
    }
}

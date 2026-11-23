using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;

namespace Camstep.Controllers;

public class EventsController : AppController
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db) => _db = db;

    public IActionResult Create(DateTime? date, string? returnUrl)
    {
        return View(new EventFormModel
        {
            Date = (date ?? DateTime.Today).Date,
            ReturnUrl = returnUrl ?? CalendarUrl(date ?? DateTime.Today)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormModel form)
    {
        if (!ModelState.IsValid) return View(form);

        var calendarEvent = new CalendarEvent { UserId = CurrentUserId };
        form.ApplyTo(calendarEvent);

        _db.CalendarEvents.Add(calendarEvent);
        await _db.SaveChangesAsync();

        TempData["Notice"] = "Evento creado.";
        return RedirectBack(form.ReturnUrl, "Index", "Calendar");
    }

    public async Task<IActionResult> Edit(int id, string? returnUrl)
    {
        var item = await _db.CalendarEvents.FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);
        if (item == null) return NotFound();

        return View(EventFormModel.From(item, returnUrl ?? CalendarUrl(item.StartsAt)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EventFormModel form)
    {
        if (!ModelState.IsValid) return View(form);

        var item = await _db.CalendarEvents.FirstOrDefaultAsync(e => e.Id == form.Id && e.UserId == CurrentUserId);
        if (item == null) return NotFound();

        form.ApplyTo(item);
        await _db.SaveChangesAsync();

        TempData["Notice"] = "Evento actualizado.";
        return RedirectBack(form.ReturnUrl, "Index", "Calendar");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var item = await _db.CalendarEvents.FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);

        if (item != null)
        {
            _db.CalendarEvents.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Notice"] = "Evento eliminado.";
        }

        return RedirectBack(returnUrl, "Index", "Calendar");
    }

    private string CalendarUrl(DateTime date) =>
        Url.Action("Index", "Calendar", new { year = date.Year, month = date.Month, day = date.Day }) ?? "/Calendar";
}

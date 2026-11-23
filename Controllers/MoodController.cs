using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;

namespace Camstep.Controllers;

public class MoodController : AppController
{
    private readonly AppDbContext _db;

    public MoodController(AppDbContext db) => _db = db;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int mood, string? note, string? returnUrl)
    {
        if (mood is < 1 or > 5)
        {
            TempData["Error"] = "Elige cómo te sientes.";
            return RedirectBack(returnUrl, "Index", "Home");
        }

        var userId = CurrentUserId;
        var today = DateTime.Today;
        note = (note ?? "").Trim();
        if (note.Length > 200) note = note[..200];

        var entry = await _db.MoodEntries.FirstOrDefaultAsync(m => m.UserId == userId && m.Date == today);
        if (entry == null)
        {
            entry = new MoodEntry { UserId = userId, Date = today };
            _db.MoodEntries.Add(entry);
        }
        entry.Mood = mood;
        if (note.Length > 0 || entry.Note.Length == 0) entry.Note = note;
        await _db.SaveChangesAsync();

        TempData["Notice"] = mood >= 4
            ? $"{MoodEntry.EmojiFor(mood)} ¡Qué bueno! Guarda ese ánimo."
            : mood == 3
                ? "😐 Anotado. Un día normal también está bien."
                : $"{MoodEntry.EmojiFor(mood)} Anotado. Sé amable contigo hoy; mañana el sendero sigue ahí.";
        return RedirectBack(returnUrl, "Index", "Home");
    }
}

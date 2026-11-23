using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Services;

namespace Camstep.Controllers;

public class QuotesController : AppController
{
    private readonly AppDbContext _db;

    public QuotesController(AppDbContext db) => _db = db;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Next()
    {
        var userId = CurrentUserId;
        var pref = await PreferenceSession.GetOrCreateAsync(_db, userId);
        if (!pref.ShowQuotes) return NoContent();

        var quote = Quotes.ForUser(userId, pref.QuoteCursor);
        pref.QuoteCursor = pref.QuoteCursor >= int.MaxValue - 1 ? 0 : pref.QuoteCursor + 1;
        await _db.SaveChangesAsync();

        return Json(new { text = quote.Text, author = quote.Author, number = pref.QuoteCursor, total = Quotes.Count });
    }
}

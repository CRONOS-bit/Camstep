using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;

namespace Camstep.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    public NotificationBellViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var count = userId == null ? 0 : await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDismissed);
        return View(count);
    }
}

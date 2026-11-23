using Microsoft.AspNetCore.Mvc;

namespace Camstep.Controllers;

public abstract class AppController : Controller
{
    protected int CurrentUserId =>
        HttpContext.Session.GetInt32("UserId")
        ?? throw new InvalidOperationException("No hay sesión activa.");

    protected IActionResult RedirectBack(string? returnUrl, string fallbackAction, string? fallbackController = null)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(fallbackAction, fallbackController);
    }
}

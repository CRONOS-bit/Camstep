using Microsoft.AspNetCore.Mvc;
using Camstep.Filters;

namespace Camstep.Controllers;

[AllowGuest]
public class PrivacyController : Controller
{
    private readonly IConfiguration _config;

    public PrivacyController(IConfiguration config) => _config = config;

    public IActionResult Index()
    {
        var email = _config["About:ContactEmail"];
        ViewData["ContactEmail"] = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        return View();
    }
}

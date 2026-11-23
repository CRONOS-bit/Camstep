using Microsoft.AspNetCore.Mvc;
using Camstep.Filters;

namespace Camstep.Controllers;

[AllowGuest]
public class AboutController : Controller
{
    private readonly IConfiguration _config;

    public AboutController(IConfiguration config) => _config = config;

    public IActionResult Index()
    {
        ViewData["ContactEmail"] = Clean(_config["About:ContactEmail"]);
        ViewData["GitHub"] = SafeUrl(_config["About:GitHub"]);
        ViewData["LinkedIn"] = SafeUrl(_config["About:LinkedIn"]);
        return View();
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? SafeUrl(string? value)
    {
        value = Clean(value);
        return value != null && Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri.ToString()
            : null;
    }
}

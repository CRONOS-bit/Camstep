using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Filters;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

[AllowGuest]
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly LoginThrottle _throttle;
    private readonly EmailSender _email;
    private readonly IWebHostEnvironment _env;
    private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
    private readonly PasswordHasher<User> _hasher = new();

    public AccountController(AppDbContext db, LoginThrottle throttle, EmailSender email, IWebHostEnvironment env,
        Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
    {
        _db = db;
        _throttle = throttle;
        _email = email;
        _env = env;
        _cache = cache;
    }

    public IActionResult Login()
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim().ToLowerInvariant();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

        var locked = _throttle.LockedFor(email, ip);
        if (locked != null)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling(locked.Value.TotalMinutes));
            ModelState.AddModelError("", $"Demasiados intentos fallidos. Por seguridad, espera {minutes} minuto(s) e inténtalo de nuevo.");
            return View(model);
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        var result = user == null
            ? PasswordVerificationResult.Failed
            : _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            var left = _throttle.RegisterFailure(email, ip);
            ModelState.AddModelError("", left > 0 && left <= 2
                ? $"Correo o contraseña incorrectos. Te quedan {left} intento(s) antes de un bloqueo de 15 minutos."
                : left == 0
                    ? "Correo o contraseña incorrectos. Por seguridad, espera 15 minutos antes de volver a intentarlo."
                    : "Correo o contraseña incorrectos.");
            return View(model);
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user!.PasswordHash = _hasher.HashPassword(user, model.Password);
            await _db.SaveChangesAsync();
        }

        _throttle.Reset(email);
        await StartSession(user!);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
        var registerKey = "register:" + ip;
        var recent = _cache.TryGetValue(registerKey, out int count) ? count : 0;
        var maxPerHour = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("Security:MaxRegistrationsPerHour", 5);
        if (recent >= maxPerHour)
        {
            ModelState.AddModelError(string.Empty, "Se crearon demasiadas cuentas desde esta conexión. Intenta de nuevo en una hora.");
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(x => x.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "Ese correo ya está registrado. Inicia sesión o usa otro correo.");
            return View(model);
        }

        var user = new User
        {
            Name = model.Name.Trim(),
            Email = email,
            PrivacyAcceptedAt = DateTime.UtcNow
        };

        user.PasswordHash = _hasher.HashPassword(user, model.Password);

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.Email), "Ese correo ya está registrado. Inicia sesión o usa otro correo.");
            return View(model);
        }

        _cache.Set(registerKey, recent + 1, TimeSpan.FromHours(1));
        await StartSession(user);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [ActionName("Logout")]
    public IActionResult LogoutGet() => RedirectToAction("Index", "Home");

    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim().ToLowerInvariant();

        var key = "reset-req:" + email;
        var count = _cache.GetOrCreate(key, e => { e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); return 0; });
        _cache.Set(key, count + 1, TimeSpan.FromHours(1));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user != null && count < 3)
        {
            var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

            var uid = user.Id;
            await _db.PasswordResetTokens
                .Where(t => t.UserId == uid && t.UsedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAtUtc, DateTime.UtcNow));

            _db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = uid,
                TokenHash = Hash(token),
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
            });
            await _db.SaveChangesAsync();

            var link = Url.Action(nameof(ResetPassword), "Account", new { token }, Request.Scheme)!;
            var sent = await _email.SendAsync(user.Email, "Recupera tu contraseña de CAMSTEP",
                $"""
                <p>Hola {System.Net.WebUtility.HtmlEncode(user.Name)},</p>
                <p>Recibimos una solicitud para cambiar tu contraseña de CAMSTEP.</p>
                <p><a href="{link}">Crear una nueva contraseña</a></p>
                <p>El enlace sirve una sola vez durante 1 hora. Si no fuiste tú, ignora este correo: tu contraseña no cambia.</p>
                """);

            if (!sent && _env.IsDevelopment())
                TempData["DevResetLink"] = link;
        }

        TempData["Notice"] = "Si ese correo está registrado, te enviamos un enlace para crear una nueva contraseña. Revisa también la carpeta de spam.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    public async Task<IActionResult> ResetPassword(string? token)
    {
        if (await ValidToken(token) == null)
        {
            TempData["Error"] = "El enlace no es válido o ya venció. Solicita uno nuevo.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Token = token! });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        var record = await ValidToken(model.Token);
        if (record == null)
        {
            TempData["Error"] = "El enlace no es válido o ya venció. Solicita uno nuevo.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        if (!ModelState.IsValid) return View(model);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == record.UserId);
        if (user == null) return RedirectToAction(nameof(Login));

        user.PasswordHash = _hasher.HashPassword(user, model.NewPassword);
        record.UsedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _throttle.Reset(user.Email);
        TempData["Notice"] = "Contraseña actualizada. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    private async Task<PasswordResetToken?> ValidToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length != 64) return null;
        var hash = Hash(token);
        var record = await _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        return record != null && record.UsedAtUtc == null && record.ExpiresAtUtc > DateTime.UtcNow ? record : null;
    }

    private static string Hash(string token) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private async Task StartSession(User user)
    {
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserName", user.Name);

        await PreferenceSession.LoadAsync(_db, HttpContext.Session, user.Id);

        TempData["Intro"] = "1";
    }
}

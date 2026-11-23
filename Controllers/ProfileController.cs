using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class ProfileController : AppController
{
    private readonly AppDbContext _db;
    private readonly UserFiles _files;
    private readonly PasswordHasher<User> _hasher = new();

    public ProfileController(AppDbContext db, UserFiles files)
    {
        _db = db;
        _files = files;
    }

    public async Task<IActionResult> Index()
    {
        var user = await CurrentUser();
        if (user == null) return Logout();

        return View(await Page(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([Bind(Prefix = "Profile")] ProfileViewModel model)
    {
        var user = await CurrentUser();
        if (user == null) return Logout();

        ClearOtherForms("Profile.");
        var email = (model.Email ?? "").Trim().ToLowerInvariant();
        var emailChanged = email != user.Email;

        if (ModelState.IsValid && emailChanged)
        {
            if (string.IsNullOrEmpty(model.CurrentPassword) || !Verify(user, model.CurrentPassword))
                ModelState.AddModelError("Profile.CurrentPassword", "Para cambiar el correo escribe tu contraseña actual correctamente.");
            else if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != user.Id))
                ModelState.AddModelError("Profile.Email", "Ese correo ya lo usa otra cuenta.");
        }

        if (!ModelState.IsValid)
        {
            var page = await Page(user);
            page.Profile = model;
            page.Profile.CreatedAt = user.CreatedAt;
            return View("Index", page);
        }

        user.Name = model.Name.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();

        HttpContext.Session.SetString("UserName", user.Name);
        TempData["Notice"] = emailChanged ? "Datos guardados. Desde ahora inicias sesión con tu nuevo correo." : "Datos guardados.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordViewModel model)
    {
        var user = await CurrentUser();
        if (user == null) return Logout();

        ClearOtherForms("Password.");
        if (ModelState.IsValid && !Verify(user, model.CurrentPassword))
            ModelState.AddModelError("Password.CurrentPassword", "La contraseña actual no es correcta.");

        if (ModelState.IsValid && model.NewPassword == model.CurrentPassword)
            ModelState.AddModelError("Password.NewPassword", "La nueva contraseña debe ser distinta de la actual.");

        if (!ModelState.IsValid)
            return View("Index", await Page(user));

        user.PasswordHash = _hasher.HashPassword(user, model.NewPassword);
        await _db.SaveChangesAsync();

        TempData["Notice"] = "Contraseña actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([Bind(Prefix = "Delete")] DeleteAccountViewModel model)
    {
        var user = await CurrentUser();
        if (user == null) return Logout();

        ClearOtherForms("Delete.");
        if (ModelState.IsValid && (model.Confirmation ?? "").Trim().ToUpperInvariant() != "ELIMINAR")
            ModelState.AddModelError("Delete.Confirmation", "Escribe la palabra ELIMINAR para confirmar.");

        if (ModelState.IsValid && !Verify(user, model.Password))
            ModelState.AddModelError("Delete.Password", "La contraseña no es correcta.");

        if (!ModelState.IsValid)
            return View("Index", await Page(user));

        var id = user.Id;

        await using (var tx = await _db.Database.BeginTransactionAsync())
        {
            await _db.Tasks.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.Expenses.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.Incomes.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.Budgets.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.UserCategories.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.GoalSteps.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.Goals.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.CalendarEvents.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.PlaylistSongs.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.Playlists.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.Songs.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.Notifications.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.PasswordResetTokens.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.UserPreferences.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _db.Users.Where(x => x.Id == id).ExecuteDeleteAsync();
            await tx.CommitAsync();
        }

        _files.DeleteAllFor(id);

        HttpContext.Session.Clear();
        TempData["Notice"] = "Tu cuenta y todos tus datos fueron eliminados.";
        return RedirectToAction("Login", "Account");
    }

    private Task<User?> CurrentUser() => _db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId);

    private IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login", "Account");
    }

    private bool Verify(User user, string password) =>
        _hasher.VerifyHashedPassword(user, user.PasswordHash, password ?? "") != PasswordVerificationResult.Failed;

    private void ClearOtherForms(string keepPrefix)
    {
        foreach (var key in ModelState.Keys.Where(k => !k.StartsWith(keepPrefix)).ToList())
            ModelState.Remove(key);
    }

    private async Task<ProfilePageViewModel> Page(User user)
    {
        var id = user.Id;
        return new ProfilePageViewModel
        {
            Profile = new ProfileViewModel { Name = user.Name, Email = user.Email, CreatedAt = user.CreatedAt },
            Counts = new Dictionary<string, int>
            {
                ["tareas"] = await _db.Tasks.CountAsync(x => x.UserId == id),
                ["eventos"] = await _db.CalendarEvents.CountAsync(x => x.UserId == id),
                ["gastos"] = await _db.Expenses.CountAsync(x => x.UserId == id),
                ["ingresos"] = await _db.Incomes.CountAsync(x => x.UserId == id),
                ["metas"] = await _db.Goals.CountAsync(x => x.UserId == id),
                ["canciones"] = await _db.Songs.CountAsync(x => x.UserId == id)
            }
        };
    }
}

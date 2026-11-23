using Microsoft.AspNetCore.Mvc;
using Camstep.Data;
using Camstep.Services;

namespace Camstep.Controllers;

public class CustomizationController : AppController
{
    private const long MaxBackgroundBytes = 10 * 1024 * 1024;
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly AppDbContext _db;
    private readonly UserFiles _files;

    public CustomizationController(AppDbContext db, UserFiles files)
    {
        _db = db;
        _files = files;
    }

    public async Task<IActionResult> Index()
    {
        var preference = await PreferenceSession.GetOrCreateAsync(_db, CurrentUserId);

        PreferenceSession.Store(HttpContext.Session, preference);

        return View(preference);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(string theme)
    {
        if (!PreferenceSession.Themes.Contains(theme, StringComparer.Ordinal))
            return RedirectToAction(nameof(Index));

        var preference = await PreferenceSession.GetOrCreateAsync(_db, CurrentUserId);
        preference.Theme = theme;
        preference.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        PreferenceSession.Store(HttpContext.Session, preference);
        TempData["Notice"] = "Tema aplicado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxBackgroundBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadBackground(IFormFile? image)
    {
        if (image == null || image.Length == 0 || image.Length > MaxBackgroundBytes)
        {
            TempData["Error"] = "Selecciona una imagen de hasta 10 MB.";
            return RedirectToAction(nameof(Index));
        }

        var extension = Path.GetExtension(Path.GetFileName(image.FileName)).ToLowerInvariant();

        if (!ImageExtensions.Contains(extension))
        {
            TempData["Error"] = "Usa una imagen JPG, PNG o WEBP.";
            return RedirectToAction(nameof(Index));
        }

        if (!await FileSignature.IsValidAsync(image, extension))
        {
            TempData["Error"] = "El archivo no parece una imagen válida. Prueba con otra imagen.";
            return RedirectToAction(nameof(Index));
        }

        var userId = CurrentUserId;
        var name = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(_files.Folder(userId, "backgrounds"), name);

        await using (var stream = System.IO.File.Create(path))
        {
            await image.CopyToAsync(stream);
        }

        var preference = await PreferenceSession.GetOrCreateAsync(_db, userId);
        var oldFile = preference.BackgroundFileName;

        preference.BackgroundFileName = name;
        preference.BackgroundBlur = 0;
        preference.BackgroundZoom = 100;
        preference.BackgroundPositionX = 50;
        preference.BackgroundPositionY = 50;
        preference.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (oldFile != null && oldFile != name)
            _files.TryDelete(userId, "backgrounds", oldFile);

        PreferenceSession.Store(HttpContext.Session, preference);
        TempData["Notice"] = "Fondo actualizado. Ajusta el desenfoque, el zoom y la posición abajo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAdjustments(int blur, int zoom, int positionX, int positionY, bool showRain)
    {
        var preference = await PreferenceSession.GetOrCreateAsync(_db, CurrentUserId);

        preference.BackgroundBlur = Math.Clamp(blur, 0, 20);
        preference.BackgroundZoom = Math.Clamp(zoom, 100, 200);
        preference.BackgroundPositionX = Math.Clamp(positionX, 0, 100);
        preference.BackgroundPositionY = Math.Clamp(positionY, 0, 100);
        preference.ShowRain = showRain;
        preference.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        PreferenceSession.Store(HttpContext.Session, preference);
        TempData["Notice"] = "Ajustes del fondo guardados.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetBackground()
    {
        var userId = CurrentUserId;
        var preference = await PreferenceSession.GetOrCreateAsync(_db, userId);

        _files.TryDelete(userId, "backgrounds", preference.BackgroundFileName);

        preference.BackgroundFileName = null;
        preference.BackgroundBlur = 0;
        preference.BackgroundZoom = 100;
        preference.BackgroundPositionX = 50;
        preference.BackgroundPositionY = 50;
        preference.ShowRain = true;
        preference.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        PreferenceSession.Store(HttpContext.Session, preference);
        TempData["Notice"] = "Volviste al fondo predeterminado de lluvia.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAmbient(bool ambientMusic, int ambientVolume)
    {
        var preference = await PreferenceSession.GetOrCreateAsync(_db, CurrentUserId);

        preference.AmbientMusic = ambientMusic;
        preference.AmbientVolume = Math.Clamp(ambientVolume, 5, 100);
        preference.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        PreferenceSession.Store(HttpContext.Session, preference);
        TempData["Notice"] = ambientMusic ? "Música ambiente activada." : "Música ambiente desactivada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveQuotes(bool showQuotes)
    {
        var preference = await PreferenceSession.GetOrCreateAsync(_db, CurrentUserId);
        preference.ShowQuotes = showQuotes;
        preference.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        PreferenceSession.Store(HttpContext.Session, preference);
        TempData["Notice"] = showQuotes ? "Frases del camino activadas." : "Frases del camino desactivadas.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Background()
    {
        var userId = CurrentUserId;
        var preference = await PreferenceSession.GetOrCreateAsync(_db, userId);
        var path = _files.SafePath(userId, "backgrounds", preference.BackgroundFileName);

        if (path == null || !System.IO.File.Exists(path)) return NotFound();

        Response.Headers.CacheControl = "private, max-age=31536000";
        return PhysicalFile(path, UserFiles.ContentTypeFor(Path.GetExtension(path)));
    }
}

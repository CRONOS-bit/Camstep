using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class MusicController : AppController
{
    private const long MaxSongBytes = 50 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".flac" };

    private readonly AppDbContext _db;
    private readonly UserFiles _files;

    public MusicController(AppDbContext db, UserFiles files)
    {
        _db = db;
        _files = files;
    }

    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;
        var songs = await _db.Songs.Where(s => s.UserId == userId).ToListAsync();
        var playlists = await _db.Playlists.Where(p => p.UserId == userId).ToListAsync();
        var items = await _db.PlaylistSongs.Where(p => p.UserId == userId).ToListAsync();
        var byId = songs.ToDictionary(s => s.Id);

        var soundCloud = await _db.SoundCloudLinks.Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Id).ToListAsync();

        return View(new MusicViewModel
        {
            SoundCloud = soundCloud,
            Songs = songs.OrderBy(s => s.Title, StringComparer.CurrentCultureIgnoreCase).ToList(),
            Playlists = playlists.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(p => new PlaylistView
                {
                    Playlist = p,
                    Items = items.Where(i => i.PlaylistId == p.Id && byId.ContainsKey(i.SongId))
                        .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
                        .Select(i => (i, byId[i.SongId])).ToList()
                }).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Library(int? playlist)
    {
        var userId = CurrentUserId;
        var songs = await _db.Songs.Where(s => s.UserId == userId).ToListAsync();
        IEnumerable<Song> ordered = songs.OrderBy(s => s.Title, StringComparer.CurrentCultureIgnoreCase);
        string? name = null;

        if (playlist != null)
        {
            var list = await _db.Playlists.FirstOrDefaultAsync(p => p.Id == playlist && p.UserId == userId);
            if (list != null)
            {
                name = list.Name;
                var items = await _db.PlaylistSongs.Where(i => i.PlaylistId == list.Id && i.UserId == userId).ToListAsync();
                var byId = songs.ToDictionary(s => s.Id);
                ordered = items.OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
                    .Where(i => byId.ContainsKey(i.SongId)).Select(i => byId[i.SongId]);
            }
        }

        return Json(new
        {
            playlist = name == null ? (int?)null : playlist,
            name,
            songs = ordered.Select(s => new { id = s.Id, title = s.Title, url = Url.Action(nameof(Stream), new { id = s.Id }) })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSoundCloud(string? url, string? title)
    {
        var userId = CurrentUserId;
        var result = SoundCloudUrl.Normalize(url);
        if (!result.Ok)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index), "Music", "soundcloud");
        }
        if (await _db.SoundCloudLinks.CountAsync(s => s.UserId == userId) >= 100)
        {
            TempData["Error"] = "Puedes guardar hasta 100 enlaces de SoundCloud.";
            return RedirectToAction(nameof(Index), "Music", "soundcloud");
        }
        if (await _db.SoundCloudLinks.AnyAsync(s => s.UserId == userId && s.Url == result.Url))
        {
            TempData["Error"] = "Ese enlace ya está en tu lista de SoundCloud.";
            return RedirectToAction(nameof(Index), "Music", "soundcloud");
        }

        title = (title ?? "").Trim();
        if (title.Length > 120) title = title[..120];
        var link = new SoundCloudLink
        {
            UserId = userId,
            Url = result.Url,
            Kind = result.Kind,
            Title = title.Length > 0 ? title : result.SuggestedTitle
        };
        _db.SoundCloudLinks.Add(link);
        await _db.SaveChangesAsync();
        TempData["Notice"] = $"«{link.Title}» guardado. Pulsa ▶ para escucharlo.";
        return RedirectToAction(nameof(Index), "Music", "soundcloud");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameSoundCloud(int id, string? title)
    {
        var link = await _db.SoundCloudLinks.FirstOrDefaultAsync(s => s.Id == id && s.UserId == CurrentUserId);
        if (link == null) return NotFound();
        title = (title ?? "").Trim();
        if (title.Length is 0 or > 120)
        {
            TempData["Error"] = "El nombre debe tener entre 1 y 120 caracteres.";
            return RedirectToAction(nameof(Index), "Music", "soundcloud");
        }
        link.Title = title;
        await _db.SaveChangesAsync();
        TempData["Notice"] = "Nombre actualizado.";
        return RedirectToAction(nameof(Index), "Music", "soundcloud");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSoundCloud(int id)
    {
        var link = await _db.SoundCloudLinks.FirstOrDefaultAsync(s => s.Id == id && s.UserId == CurrentUserId);
        if (link == null) return NotFound();
        _db.SoundCloudLinks.Remove(link);
        await _db.SaveChangesAsync();
        TempData["Notice"] = $"«{link.Title}» quitado de tu lista de SoundCloud.";
        return RedirectToAction(nameof(Index), "Music", "soundcloud");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlaylist(string? name)
    {
        var userId = CurrentUserId;
        name = (name ?? "").Trim();
        if (name.Length is 0 or > 60)
        {
            TempData["Error"] = "El nombre de la lista debe tener entre 1 y 60 caracteres.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.Playlists.CountAsync(p => p.UserId == userId) >= 50)
        {
            TempData["Error"] = "Puedes tener hasta 50 listas.";
            return RedirectToAction(nameof(Index));
        }

        _db.Playlists.Add(new Playlist { UserId = userId, Name = name });
        await _db.SaveChangesAsync();
        TempData["Notice"] = $"Lista «{name}» creada. Agrégale canciones desde tu biblioteca.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenamePlaylist(int id, string? name)
    {
        var list = await _db.Playlists.FirstOrDefaultAsync(p => p.Id == id && p.UserId == CurrentUserId);
        if (list == null) return NotFound();
        name = (name ?? "").Trim();
        if (name.Length is 0 or > 60)
        {
            TempData["Error"] = "El nombre de la lista debe tener entre 1 y 60 caracteres.";
            return RedirectToAction(nameof(Index));
        }
        list.Name = name;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePlaylist(int id)
    {
        var userId = CurrentUserId;
        var list = await _db.Playlists.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (list == null) return NotFound();

        await _db.PlaylistSongs.Where(i => i.PlaylistId == id && i.UserId == userId).ExecuteDeleteAsync();
        _db.Playlists.Remove(list);
        await _db.SaveChangesAsync();
        TempData["Notice"] = $"Lista «{list.Name}» eliminada. Las canciones siguen en tu biblioteca.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToPlaylist(int songId, int playlistId)
    {
        var userId = CurrentUserId;
        var song = await _db.Songs.FirstOrDefaultAsync(s => s.Id == songId && s.UserId == userId);
        var list = await _db.Playlists.FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == userId);
        if (song == null || list == null) return NotFound();

        if (await _db.PlaylistSongs.AnyAsync(i => i.PlaylistId == playlistId && i.SongId == songId))
        {
            TempData["Error"] = $"«{song.Title}» ya está en «{list.Name}».";
            return RedirectToAction(nameof(Index));
        }

        var next = await _db.PlaylistSongs.Where(i => i.PlaylistId == playlistId).Select(i => (int?)i.SortOrder).MaxAsync() ?? -1;
        _db.PlaylistSongs.Add(new PlaylistSong { PlaylistId = playlistId, SongId = songId, UserId = userId, SortOrder = next + 1 });
        await _db.SaveChangesAsync();
        TempData["Notice"] = $"«{song.Title}» se agregó a «{list.Name}».";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromPlaylist(int itemId)
    {
        await _db.PlaylistSongs.Where(i => i.Id == itemId && i.UserId == CurrentUserId).ExecuteDeleteAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveInPlaylist(int itemId, int direction)
    {
        var userId = CurrentUserId;
        var item = await _db.PlaylistSongs.FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId);
        if (item == null) return NotFound();

        var items = (await _db.PlaylistSongs.Where(i => i.PlaylistId == item.PlaylistId && i.UserId == userId).ToListAsync())
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToList();
        var index = items.FindIndex(i => i.Id == itemId);
        var target = index + Math.Sign(direction);

        if (target >= 0 && target < items.Count)
        {
            (items[index], items[target]) = (items[target], items[index]);
            for (var i = 0; i < items.Count; i++) items[i].SortOrder = i;
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxSongBytes + 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile? audio)
    {
        if (audio == null || audio.Length == 0 || audio.Length > MaxSongBytes)
        {
            TempData["Error"] = "Elige un archivo de audio de hasta 50 MB.";
            return RedirectToAction(nameof(Index));
        }

        var originalName = Path.GetFileName(audio.FileName);
        var extension = Path.GetExtension(originalName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            TempData["Error"] = "Formato no admitido. Usa MP3, WAV, OGG, M4A, AAC o FLAC.";
            return RedirectToAction(nameof(Index));
        }

        if (!await FileSignature.IsValidAsync(audio, extension))
        {
            TempData["Error"] = "El archivo no parece un audio válido. Prueba con otro archivo.";
            return RedirectToAction(nameof(Index));
        }

        var userId = CurrentUserId;
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(_files.Folder(userId, "music"), storedName);

        await using (var stream = System.IO.File.Create(path))
        {
            await audio.CopyToAsync(stream);
        }

        var title = Path.GetFileNameWithoutExtension(originalName).Replace('_', ' ').Trim();
        if (title.Length == 0) title = "Sin título";
        if (title.Length > 150) title = title[..150];

        _db.Songs.Add(new Song
        {
            UserId = userId,
            Title = title,
            OriginalFileName = originalName.Length > 255 ? originalName[..255] : originalName,
            StoredFileName = storedName,
            ContentType = UserFiles.ContentTypeFor(extension),
            SizeBytes = audio.Length
        });
        await _db.SaveChangesAsync();

        TempData["Notice"] = $"\"{title}\" se agregó a tu biblioteca.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(int id, string? title)
    {
        var song = await _db.Songs.FirstOrDefaultAsync(s => s.Id == id && s.UserId == CurrentUserId);
        if (song == null) return NotFound();

        title = (title ?? "").Trim();
        if (title.Length == 0 || title.Length > 150)
        {
            TempData["Error"] = "El nombre debe tener entre 1 y 150 caracteres.";
            return RedirectToAction(nameof(Index));
        }

        song.Title = title;
        await _db.SaveChangesAsync();

        TempData["Notice"] = "Nombre actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId;
        var song = await _db.Songs.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (song != null)
        {
            _db.Songs.Remove(song);
            _db.PlaylistSongs.RemoveRange(_db.PlaylistSongs.Where(i => i.SongId == song.Id && i.UserId == userId));
            await _db.SaveChangesAsync();
            _files.TryDelete(userId, "music", song.StoredFileName);
            TempData["Notice"] = $"\"{song.Title}\" se eliminó de tu biblioteca.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Stream(int id)
    {
        var userId = CurrentUserId;
        var song = await _db.Songs.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (song == null) return NotFound();

        var path = _files.SafePath(userId, "music", song.StoredFileName);
        if (path == null || !System.IO.File.Exists(path)) return NotFound();

        Response.Headers.CacheControl = "private, max-age=86400";
        return PhysicalFile(path, song.ContentType, enableRangeProcessing: true);
    }
}

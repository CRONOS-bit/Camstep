using System.Globalization;
using System.Text.RegularExpressions;

namespace Camstep.Services;

public static partial class SoundCloudUrl
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "discover", "search", "stream", "you", "upload", "charts", "pages", "settings", "messages",
        "notifications", "terms-of-use", "jobs", "imprint", "mobile", "pro", "go", "people", "popular", "tags", "signin", "logout"
    };

    private static readonly HashSet<string> ProfileTabs = new(StringComparer.OrdinalIgnoreCase)
    {
        "tracks", "albums", "sets", "reposts", "likes", "popular-tracks", "spotlight"
    };

    [GeneratedRegex("^[A-Za-z0-9_-]{1,120}$")]
    private static partial Regex Segment();

    public record Result(bool Ok, string Url, string Kind, string SuggestedTitle, string? Error);

    public static Result Normalize(string? input)
    {
        var text = (input ?? "").Trim();
        if (text.Length == 0)
            return Fail("Pega el enlace de SoundCloud.");
        if (text.Length > 400)
            return Fail("El enlace es demasiado largo.");

        if (!text.Contains("://", StringComparison.Ordinal))
            text = "https://" + text;

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return Fail("Ese enlace no es válido. Cópialo desde SoundCloud con el botón Compartir.");

        var host = uri.Host.ToLowerInvariant();
        if (host is "on.soundcloud.com" or "soundcloud.app.goo.gl")
            return Fail("Ese es un enlace corto. Ábrelo en el navegador y copia la dirección completa (la que empieza por soundcloud.com/...).");
        if (host is not ("soundcloud.com" or "www.soundcloud.com" or "m.soundcloud.com"))
            return Fail("Solo se aceptan enlaces de soundcloud.com.");

        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Length > 4 || parts.Any(p => !Segment().IsMatch(p)) || Reserved.Contains(parts[0]))
            return Fail("Pega el enlace de una canción, una playlist o un perfil de SoundCloud.");

        string kind;
        string title;
        if (parts.Length == 1 || (parts.Length == 2 && ProfileTabs.Contains(parts[1])))
        {
            kind = "artist";
            title = Pretty(parts[0]);
        }
        else if (parts[1].Equals("sets", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
        {
            kind = "playlist";
            title = Pretty(parts[2]) + " · " + Pretty(parts[0]);
        }
        else if (parts.Length <= 3)
        {
            if (parts.Length == 3 && !parts[2].StartsWith("s-", StringComparison.OrdinalIgnoreCase))
                return Fail("Pega el enlace de una canción, una playlist o un perfil de SoundCloud.");
            kind = "track";
            title = Pretty(parts[1]) + " · " + Pretty(parts[0]);
        }
        else
        {
            return Fail("Pega el enlace de una canción, una playlist o un perfil de SoundCloud.");
        }

        var clean = "https://soundcloud.com/" + string.Join('/', parts);
        return new Result(true, clean, kind, title.Length > 120 ? title[..120] : title, null);
    }

    private static Result Fail(string error) => new(false, "", "", "", error);

    private static string Pretty(string slug)
    {
        var words = slug.Replace('_', ' ').Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(w => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(w.ToLowerInvariant())));
    }
}

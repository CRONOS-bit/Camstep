namespace Camstep.Services;

public class UserFiles
{
    private readonly string _root;

    public UserFiles(AppPaths paths)
    {
        _root = paths.Uploads;
    }

    public string Folder(int userId, string kind)
    {
        var folder = Path.Combine(_root, userId.ToString(), kind);
        Directory.CreateDirectory(folder);
        return folder;
    }

    public string? SafePath(int userId, string kind, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        if (fileName != Path.GetFileName(fileName)) return null;
        if (fileName.Contains("..")) return null;

        var full = Path.GetFullPath(Path.Combine(Folder(userId, kind), fileName));
        var folder = Path.GetFullPath(Folder(userId, kind)) + Path.DirectorySeparatorChar;

        return full.StartsWith(folder, StringComparison.Ordinal) ? full : null;
    }

    public void TryDelete(int userId, string kind, string? fileName)
    {
        var path = SafePath(userId, kind, fileName);
        try
        {
            if (path != null && File.Exists(path)) File.Delete(path);
        }
        catch (IOException) { }
    }

    public void DeleteAllFor(int userId)
    {
        var folder = Path.Combine(_root, userId.ToString());
        try
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
        catch (IOException) { }
    }

    public static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".ogg" => "audio/ogg",
        ".m4a" => "audio/mp4",
        ".aac" => "audio/aac",
        ".flac" => "audio/flac",
        _ => "application/octet-stream"
    };
}

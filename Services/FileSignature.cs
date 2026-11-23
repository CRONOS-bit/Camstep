namespace Camstep.Services;

public static class FileSignature
{
    public static async Task<bool> IsValidAsync(IFormFile file, string extension)
    {
        var header = new byte[16];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        if (read < 4) return false;

        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            ".webp" => Ascii(header, 0, "RIFF") && read >= 12 && Ascii(header, 8, "WEBP"),
            ".mp3" => Ascii(header, 0, "ID3") || (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0),
            ".wav" => Ascii(header, 0, "RIFF") && read >= 12 && Ascii(header, 8, "WAVE"),
            ".ogg" => Ascii(header, 0, "OggS"),
            ".flac" => Ascii(header, 0, "fLaC"),
            ".m4a" => read >= 8 && Ascii(header, 4, "ftyp"),
            ".aac" => (header[0] == 0xFF && (header[1] & 0xF6) == 0xF0) || Ascii(header, 0, "ADIF") || Ascii(header, 0, "ID3"),
            _ => false
        };
    }

    private static bool Ascii(byte[] data, int offset, string text)
    {
        if (offset + text.Length > data.Length) return false;
        for (var i = 0; i < text.Length; i++)
            if (data[offset + i] != (byte)text[i]) return false;
        return true;
    }
}

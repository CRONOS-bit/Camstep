namespace Camstep.Models;

public class SoundCloudLink
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";

    public string Url { get; set; } = "";

    public string Kind { get; set; } = "track";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string KindText => Kind switch
    {
        "playlist" => "Playlist",
        "artist" => "Perfil",
        _ => "Canción"
    };
}

using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;

namespace Camstep.Services;

public static class PreferenceSession
{
    public const string Theme = "Theme";
    public const string BackgroundImage = "BackgroundImage";
    public const string BgBlur = "BgBlur";
    public const string BgZoom = "BgZoom";
    public const string BgPosX = "BgPosX";
    public const string BgPosY = "BgPosY";
    public const string ShowRain = "ShowRain";
    public const string AmbientMusic = "AmbientMusic";
    public const string AmbientVolume = "AmbientVolume";
    public const string ShowQuotes = "ShowQuotes";

    public static readonly string[] Themes = { "Light", "Black", "Midnight", "Galaxy", "Ocean", "Nature" };

    public static async Task<UserPreference> GetOrCreateAsync(AppDbContext db, int userId)
    {
        var preference = await db.UserPreferences.FirstOrDefaultAsync(x => x.UserId == userId);

        if (preference == null)
        {
            preference = new UserPreference { UserId = userId };
            db.UserPreferences.Add(preference);
            await db.SaveChangesAsync();
        }

        return preference;
    }

    public static async Task LoadAsync(AppDbContext db, ISession session, int userId)
    {
        var preference = await GetOrCreateAsync(db, userId);
        Store(session, preference);
    }

    public static void Store(ISession session, UserPreference p)
    {
        session.SetString(Theme, Themes.Contains(p.Theme) ? p.Theme : "Light");

        if (string.IsNullOrWhiteSpace(p.BackgroundFileName))
            session.Remove(BackgroundImage);
        else
            session.SetString(BackgroundImage, "/Customization/Background?v=" + Uri.EscapeDataString(p.BackgroundFileName));

        session.SetInt32(BgBlur, Math.Clamp(p.BackgroundBlur, 0, 20));
        session.SetInt32(BgZoom, Math.Clamp(p.BackgroundZoom, 100, 200));
        session.SetInt32(BgPosX, Math.Clamp(p.BackgroundPositionX, 0, 100));
        session.SetInt32(BgPosY, Math.Clamp(p.BackgroundPositionY, 0, 100));
        session.SetInt32(ShowRain, p.ShowRain ? 1 : 0);
        session.SetInt32(AmbientMusic, p.AmbientMusic ? 1 : 0);
        session.SetInt32(AmbientVolume, Math.Clamp(p.AmbientVolume, 5, 100));
        session.SetInt32(ShowQuotes, p.ShowQuotes ? 1 : 0);
    }
}

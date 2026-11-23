namespace Camstep.Models;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Kind { get; set; } = "task";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Link { get; set; } = "";
    public string SourceKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; }

    public bool IsDismissed { get; set; }
}

public class Playlist
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PlaylistSong
{
    public int Id { get; set; }
    public int PlaylistId { get; set; }
    public int SongId { get; set; }
    public int UserId { get; set; }
    public int SortOrder { get; set; }
}

public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class NotificationsViewModel
{
    public List<Notification> Items { get; set; } = new();
    public UserPreference Settings { get; set; } = new();
    public string Filter { get; set; } = "all";
    public int UnreadCount { get; set; }
}

public class SearchResult
{
    public string Kind { get; set; } = "";
    public string KindLabel { get; set; } = "";
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime? Date { get; set; }
    public string Url { get; set; } = "";
    public int Score { get; set; }
}

public class SearchViewModel
{
    public string Q { get; set; } = "";
    public string Type { get; set; } = "all";
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string Sort { get; set; } = "relevance";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public Dictionary<string, int> CountByType { get; set; } = new();
    public List<SearchResult> Results { get; set; } = new();
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
}

public class ReportViewModel
{
    public string Period { get; set; } = "month";
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string Label { get; set; } = "";
    public string PrevLabel { get; set; } = "";
    public DateTime Anchor { get; set; }

    public int TasksDue { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksPending { get; set; }
    public int TasksOverdue { get; set; }
    public int CompletionRate => TasksDue == 0 ? 0 : (int)Math.Round(TasksCompleted * 100.0 / TasksDue);
    public int PrevCompletionRate { get; set; }
    public int EventsCount { get; set; }
    public List<(string Label, int Completed)> WeeklyProductivity { get; set; } = new();
    public List<(string Label, int Completed)> MonthlyProductivity { get; set; } = new();

    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance => Income - Expense;
    public decimal PrevIncome { get; set; }
    public decimal PrevExpense { get; set; }
    public List<CategoryTotal> ExpenseByCategory { get; set; } = new();

    public List<Goal> Goals { get; set; } = new();
    public int GoalsCompleted { get; set; }
    public int GoalsOverdue { get; set; }
    public int GoalsActive { get; set; }

    public List<MoodEntry> MoodDays { get; set; } = new();
    public DateTime MoodFrom { get; set; }
    public DateTime MoodTo { get; set; }
    public double? MoodAverage => MoodDays.Count > 0 ? MoodDays.Average(m => m.Mood) : null;
    public int FocusMinutes { get; set; }
}

public class PlaylistView
{
    public Playlist Playlist { get; set; } = new();
    public List<(PlaylistSong Item, Song Song)> Items { get; set; } = new();
}

public class MusicViewModel
{
    public List<Song> Songs { get; set; } = new();
    public List<PlaylistView> Playlists { get; set; } = new();
    public List<SoundCloudLink> SoundCloud { get; set; } = new();
}

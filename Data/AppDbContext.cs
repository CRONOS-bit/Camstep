using Microsoft.EntityFrameworkCore;
using Camstep.Models;

namespace Camstep.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<Goal> Goals => Set<Goal>();

    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    public DbSet<Song> Songs => Set<Song>();

    public DbSet<Income> Incomes => Set<Income>();

    public DbSet<Budget> Budgets => Set<Budget>();

    public DbSet<UserCategory> UserCategories => Set<UserCategory>();

    public DbSet<GoalStep> GoalSteps => Set<GoalStep>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<Playlist> Playlists => Set<Playlist>();

    public DbSet<PlaylistSong> PlaylistSongs => Set<PlaylistSong>();
    public DbSet<SoundCloudLink> SoundCloudLinks => Set<SoundCloudLink>();
    public DbSet<FocusSession> FocusSessions => Set<FocusSession>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();
    public DbSet<MoodEntry> MoodEntries => Set<MoodEntry>();
    public DbSet<WeeklyReview> WeeklyReviews => Set<WeeklyReview>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<UserPreference>().HasIndex(p => p.UserId).IsUnique();
        modelBuilder.Entity<CalendarEvent>().HasIndex(e => new { e.UserId, e.StartsAt });
        modelBuilder.Entity<Song>().HasIndex(s => s.UserId);
        modelBuilder.Entity<Income>().HasIndex(i => new { i.UserId, i.Date });
        modelBuilder.Entity<Budget>().HasIndex(b => new { b.UserId, b.Category }).IsUnique();
        modelBuilder.Entity<UserCategory>().HasIndex(c => new { c.UserId, c.Kind, c.Name }).IsUnique();
        modelBuilder.Entity<GoalStep>().HasIndex(s => new { s.GoalId, s.UserId });
        modelBuilder.Entity<Notification>().HasIndex(n => new { n.UserId, n.SourceKey }).IsUnique();
        modelBuilder.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead });
        modelBuilder.Entity<Playlist>().HasIndex(p => p.UserId);
        modelBuilder.Entity<PlaylistSong>().HasIndex(p => new { p.PlaylistId, p.UserId });
        modelBuilder.Entity<SoundCloudLink>().HasIndex(s => s.UserId);
        modelBuilder.Entity<SoundCloudLink>().Ignore(s => s.KindText);
        modelBuilder.Entity<FocusSession>().HasIndex(f => new { f.UserId, f.EndedAt });
        modelBuilder.Entity<Habit>().HasIndex(h => h.UserId);
        modelBuilder.Entity<HabitLog>().HasIndex(l => new { l.HabitId, l.Date }).IsUnique();
        modelBuilder.Entity<HabitLog>().HasIndex(l => l.UserId);
        modelBuilder.Entity<MoodEntry>().HasIndex(m => new { m.UserId, m.Date }).IsUnique();
        modelBuilder.Entity<WeeklyReview>().HasIndex(w => new { w.UserId, w.WeekStart }).IsUnique();
        modelBuilder.Entity<PasswordResetToken>().HasIndex(t => t.TokenHash).IsUnique();
    }
}

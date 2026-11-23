using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Camstep.Data;

public static class SchemaUpdater
{
    private sealed record Step(string Description, Func<DbConnection, bool> IsNeeded, string Sql, bool Optional = false);

    private static readonly Step[] Steps =
    {
        new("Tabla UserPreferences",
            c => !TableExists(c, "UserPreferences"),
            """
            CREATE TABLE IF NOT EXISTS UserPreferences (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Theme TEXT NOT NULL DEFAULT 'Light',
                BackgroundFileName TEXT NULL,
                BackgroundBlur INTEGER NOT NULL DEFAULT 0,
                BackgroundZoom INTEGER NOT NULL DEFAULT 100,
                BackgroundPositionX INTEGER NOT NULL DEFAULT 50,
                BackgroundPositionY INTEGER NOT NULL DEFAULT 50,
                ShowRain INTEGER NOT NULL DEFAULT 1,
                UpdatedAt TEXT NOT NULL,
                CONSTRAINT UQ_UserPreferences_UserId UNIQUE (UserId)
            );
            """),

        new("Columna UserPreferences.ShowRain",
            c => TableExists(c, "UserPreferences") && !ColumnExists(c, "UserPreferences", "ShowRain"),
            "ALTER TABLE UserPreferences ADD COLUMN ShowRain INTEGER NOT NULL DEFAULT 1;"),

        new("Tabla CalendarEvents",
            c => !TableExists(c, "CalendarEvents"),
            """
            CREATE TABLE IF NOT EXISTS CalendarEvents (
                Id INTEGER NOT NULL CONSTRAINT PK_CalendarEvents PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Description TEXT NOT NULL DEFAULT '',
                StartsAt TEXT NOT NULL,
                EndsAt TEXT NULL,
                AllDay INTEGER NOT NULL DEFAULT 0,
                Location TEXT NOT NULL DEFAULT '',
                Color TEXT NOT NULL DEFAULT 'azul',
                ReminderMinutesBefore INTEGER NULL,
                CreatedAt TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_CalendarEvents_UserId_StartsAt ON CalendarEvents (UserId, StartsAt);
            """),

        new("Tabla Songs",
            c => !TableExists(c, "Songs"),
            """
            CREATE TABLE IF NOT EXISTS Songs (
                Id INTEGER NOT NULL CONSTRAINT PK_Songs PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                OriginalFileName TEXT NOT NULL,
                StoredFileName TEXT NOT NULL,
                ContentType TEXT NOT NULL,
                SizeBytes INTEGER NOT NULL,
                UploadedAt TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Songs_UserId ON Songs (UserId);
            """),

        new("Columna Tasks.CompletedAt",
            c => !ColumnExists(c, "Tasks", "CompletedAt"),
            "ALTER TABLE Tasks ADD COLUMN CompletedAt TEXT NULL;"),

        new("Columna Goals.Category",
            c => !ColumnExists(c, "Goals", "Category"),
            "ALTER TABLE Goals ADD COLUMN Category TEXT NOT NULL DEFAULT 'Personal';"),

        new("Columna Goals.Notes",
            c => !ColumnExists(c, "Goals", "Notes"),
            "ALTER TABLE Goals ADD COLUMN Notes TEXT NOT NULL DEFAULT '';"),

        new("Tabla Incomes",
            c => !TableExists(c, "Incomes"),
            """
            CREATE TABLE IF NOT EXISTS Incomes (
                Id INTEGER NOT NULL CONSTRAINT PK_Incomes PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Description TEXT NOT NULL,
                Amount TEXT NOT NULL,
                Category TEXT NOT NULL,
                Date TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Incomes_UserId_Date ON Incomes (UserId, Date);
            """),

        new("Tabla Budgets",
            c => !TableExists(c, "Budgets"),
            """
            CREATE TABLE IF NOT EXISTS Budgets (
                Id INTEGER NOT NULL CONSTRAINT PK_Budgets PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Category TEXT NOT NULL DEFAULT '',
                Amount TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Budgets_UserId_Category ON Budgets (UserId, Category);
            """),

        new("Tabla UserCategories",
            c => !TableExists(c, "UserCategories"),
            """
            CREATE TABLE IF NOT EXISTS UserCategories (
                Id INTEGER NOT NULL CONSTRAINT PK_UserCategories PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Kind TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_UserCategories_UserId_Kind_Name ON UserCategories (UserId, Kind, Name);
            """),

        new("Tabla GoalSteps",
            c => !TableExists(c, "GoalSteps"),
            """
            CREATE TABLE IF NOT EXISTS GoalSteps (
                Id INTEGER NOT NULL CONSTRAINT PK_GoalSteps PRIMARY KEY AUTOINCREMENT,
                GoalId INTEGER NOT NULL,
                UserId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                IsDone INTEGER NOT NULL DEFAULT 0,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                CompletedAt TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_GoalSteps_GoalId_UserId ON GoalSteps (GoalId, UserId);
            """),

        new("Columna UserPreferences.NotifyTasks",
            c => TableExists(c, "UserPreferences") && !ColumnExists(c, "UserPreferences", "NotifyTasks"),
            "ALTER TABLE UserPreferences ADD COLUMN NotifyTasks INTEGER NOT NULL DEFAULT 1;"),

        new("Columna UserPreferences.NotifyEvents",
            c => TableExists(c, "UserPreferences") && !ColumnExists(c, "UserPreferences", "NotifyEvents"),
            "ALTER TABLE UserPreferences ADD COLUMN NotifyEvents INTEGER NOT NULL DEFAULT 1;"),

        new("Columna UserPreferences.NotifyGoals",
            c => TableExists(c, "UserPreferences") && !ColumnExists(c, "UserPreferences", "NotifyGoals"),
            "ALTER TABLE UserPreferences ADD COLUMN NotifyGoals INTEGER NOT NULL DEFAULT 1;"),

        new("Columna UserPreferences.NotifyBudget",
            c => TableExists(c, "UserPreferences") && !ColumnExists(c, "UserPreferences", "NotifyBudget"),
            "ALTER TABLE UserPreferences ADD COLUMN NotifyBudget INTEGER NOT NULL DEFAULT 1;"),

        new("Columna UserPreferences.TaskLeadDays",
            c => TableExists(c, "UserPreferences") && !ColumnExists(c, "UserPreferences", "TaskLeadDays"),
            "ALTER TABLE UserPreferences ADD COLUMN TaskLeadDays INTEGER NOT NULL DEFAULT 1;"),

        new("Columna UserPreferences.EventLeadMinutes",
            c => TableExists(c, "UserPreferences") && !ColumnExists(c, "UserPreferences", "EventLeadMinutes"),
            "ALTER TABLE UserPreferences ADD COLUMN EventLeadMinutes INTEGER NOT NULL DEFAULT 60;"),

        new("Columna UserPreferences.GoalLeadDays",
            c => TableExists(c, "UserPreferences") && !ColumnExists(c, "UserPreferences", "GoalLeadDays"),
            "ALTER TABLE UserPreferences ADD COLUMN GoalLeadDays INTEGER NOT NULL DEFAULT 7;"),

        new("Tabla SoundCloudLinks",
            c => !TableExists(c, "SoundCloudLinks"),
            """
            CREATE TABLE IF NOT EXISTS SoundCloudLinks (
                Id INTEGER NOT NULL CONSTRAINT PK_SoundCloudLinks PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Url TEXT NOT NULL,
                Kind TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_SoundCloudLinks_UserId ON SoundCloudLinks (UserId);
            """),

        new("Tabla FocusSessions",
            c => !TableExists(c, "FocusSessions"),
            """
            CREATE TABLE IF NOT EXISTS FocusSessions (
                Id INTEGER NOT NULL CONSTRAINT PK_FocusSessions PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                TaskId INTEGER NULL,
                TaskTitle TEXT NOT NULL,
                Minutes INTEGER NOT NULL,
                StartedAt TEXT NOT NULL,
                EndedAt TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_FocusSessions_UserId_EndedAt ON FocusSessions (UserId, EndedAt);
            """),

        new("Tabla Habits",
            c => !TableExists(c, "Habits"),
            """
            CREATE TABLE IF NOT EXISTS Habits (
                Id INTEGER NOT NULL CONSTRAINT PK_Habits PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Icon TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Habits_UserId ON Habits (UserId);
            """),

        new("Tabla HabitLogs",
            c => !TableExists(c, "HabitLogs"),
            """
            CREATE TABLE IF NOT EXISTS HabitLogs (
                Id INTEGER NOT NULL CONSTRAINT PK_HabitLogs PRIMARY KEY AUTOINCREMENT,
                HabitId INTEGER NOT NULL,
                UserId INTEGER NOT NULL,
                Date TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_HabitLogs_HabitId_Date ON HabitLogs (HabitId, Date);
            CREATE INDEX IF NOT EXISTS IX_HabitLogs_UserId ON HabitLogs (UserId);
            """),

        new("Tabla MoodEntries",
            c => !TableExists(c, "MoodEntries"),
            """
            CREATE TABLE IF NOT EXISTS MoodEntries (
                Id INTEGER NOT NULL CONSTRAINT PK_MoodEntries PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Date TEXT NOT NULL,
                Mood INTEGER NOT NULL,
                Note TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_MoodEntries_UserId_Date ON MoodEntries (UserId, Date);
            """),

        new("Tabla WeeklyReviews",
            c => !TableExists(c, "WeeklyReviews"),
            """
            CREATE TABLE IF NOT EXISTS WeeklyReviews (
                Id INTEGER NOT NULL CONSTRAINT PK_WeeklyReviews PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                WeekStart TEXT NOT NULL,
                Wins TEXT NOT NULL,
                Step1 TEXT NOT NULL,
                Step2 TEXT NOT NULL,
                Step3 TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_WeeklyReviews_UserId_WeekStart ON WeeklyReviews (UserId, WeekStart);
            """),

        new("Columna GoalSteps.CompletedAt",
            c => TableExists(c, "GoalSteps") && !ColumnExists(c, "GoalSteps", "CompletedAt"),
            "ALTER TABLE GoalSteps ADD COLUMN CompletedAt TEXT NULL;"),

        new("Columna UserPreferences.ShowQuotes",
            c => TableExists(c, "UserPreferences") && !ColumnExists(c, "UserPreferences", "ShowQuotes"),
            "ALTER TABLE UserPreferences ADD COLUMN ShowQuotes INTEGER NOT NULL DEFAULT 1;"),

        new("Columna UserPreferences.QuoteCursor",
            c => TableExists(c, "UserPreferences") && !ColumnExists(c, "UserPreferences", "QuoteCursor"),
            "ALTER TABLE UserPreferences ADD COLUMN QuoteCursor INTEGER NOT NULL DEFAULT 0;"),

        new("Columna UserPreferences.AmbientMusic",
            c => TableExists(c, "UserPreferences") && !ColumnExists(c, "UserPreferences", "AmbientMusic"),
            "ALTER TABLE UserPreferences ADD COLUMN AmbientMusic INTEGER NOT NULL DEFAULT 1;"),

        new("Columna UserPreferences.AmbientVolume",
            c => TableExists(c, "UserPreferences") && !ColumnExists(c, "UserPreferences", "AmbientVolume"),
            "ALTER TABLE UserPreferences ADD COLUMN AmbientVolume INTEGER NOT NULL DEFAULT 35;"),

        new("Tabla Notifications",
            c => !TableExists(c, "Notifications"),
            """
            CREATE TABLE IF NOT EXISTS Notifications (
                Id INTEGER NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Kind TEXT NOT NULL,
                Title TEXT NOT NULL,
                Message TEXT NOT NULL,
                Link TEXT NOT NULL,
                SourceKey TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                IsRead INTEGER NOT NULL DEFAULT 0,
                IsDismissed INTEGER NOT NULL DEFAULT 0
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Notifications_UserId_SourceKey ON Notifications (UserId, SourceKey);
            CREATE INDEX IF NOT EXISTS IX_Notifications_UserId_IsRead ON Notifications (UserId, IsRead);
            """),

        new("Tabla Playlists",
            c => !TableExists(c, "Playlists"),
            """
            CREATE TABLE IF NOT EXISTS Playlists (
                Id INTEGER NOT NULL CONSTRAINT PK_Playlists PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Playlists_UserId ON Playlists (UserId);
            """),

        new("Tabla PlaylistSongs",
            c => !TableExists(c, "PlaylistSongs"),
            """
            CREATE TABLE IF NOT EXISTS PlaylistSongs (
                Id INTEGER NOT NULL CONSTRAINT PK_PlaylistSongs PRIMARY KEY AUTOINCREMENT,
                PlaylistId INTEGER NOT NULL,
                SongId INTEGER NOT NULL,
                UserId INTEGER NOT NULL,
                SortOrder INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS IX_PlaylistSongs_PlaylistId_UserId ON PlaylistSongs (PlaylistId, UserId);
            """),

        new("Tabla PasswordResetTokens",
            c => !TableExists(c, "PasswordResetTokens"),
            """
            CREATE TABLE IF NOT EXISTS PasswordResetTokens (
                Id INTEGER NOT NULL CONSTRAINT PK_PasswordResetTokens PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                TokenHash TEXT NOT NULL,
                ExpiresAtUtc TEXT NOT NULL,
                UsedAtUtc TEXT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_PasswordResetTokens_TokenHash ON PasswordResetTokens (TokenHash);
            """),

        new("Índice único Users.Email",
            c => !IndexExists(c, "IX_Users_Email"),
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email ON Users (Email);",
            Optional: true),
    };

    public static void Apply(AppDbContext db, string contentRoot, ILogger logger)
    {
        var connection = db.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;
        if (!wasOpen) connection.Open();

        try
        {
            var pending = Steps.Where(s => s.IsNeeded(connection)).ToList();
            if (pending.Count == 0) return;

            Backup(connection, contentRoot, logger);

            foreach (var step in pending)
            {
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = step.Sql;
                    command.ExecuteNonQuery();
                    logger.LogInformation("Esquema actualizado: {Step}", step.Description);
                }
                catch (SqliteException ex) when (step.Optional)
                {
                    logger.LogWarning(ex, "No se pudo aplicar el paso opcional '{Step}'.", step.Description);
                }
            }
        }
        finally
        {
            if (!wasOpen) connection.Close();
        }
    }

    private static void Backup(DbConnection connection, string contentRoot, ILogger logger)
    {
        if (!TableExists(connection, "Users")) return;

        var folder = Path.Combine(contentRoot, "backups");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"camstep-{DateTime.Now:yyyyMMdd-HHmmss}.db");

        using var command = connection.CreateCommand();
        command.CommandText = "VACUUM INTO $file;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$file";
        parameter.Value = file;
        command.Parameters.Add(parameter);
        command.ExecuteNonQuery();

        logger.LogInformation("Copia de respaldo creada antes de actualizar el esquema: {File}", file);
    }

    private static bool TableExists(DbConnection c, string table) =>
        Scalar(c, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $p;", table) > 0;

    private static bool IndexExists(DbConnection c, string index) =>
        Scalar(c, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = $p;", index) > 0;

    private static bool ColumnExists(DbConnection c, string table, string column) =>
        Scalar(c, $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = $p;", column) > 0;

    private static long Scalar(DbConnection c, string sql, string value)
    {
        using var command = c.CreateCommand();
        command.CommandText = sql;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$p";
        parameter.Value = value;
        command.Parameters.Add(parameter);
        return Convert.ToInt64(command.ExecuteScalar());
    }
}

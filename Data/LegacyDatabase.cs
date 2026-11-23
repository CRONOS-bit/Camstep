using Microsoft.Data.Sqlite;

namespace Camstep.Data;

public static class LegacyDatabase
{
    public const string LegacyFileName = "vidaenorden.db";

    public static bool MoveToNewName(string databasePath)
    {
        if (File.Exists(databasePath)) return false;

        var folder = Path.GetDirectoryName(databasePath);
        if (string.IsNullOrEmpty(folder)) return false;

        var legacy = Path.Combine(folder, LegacyFileName);
        if (!File.Exists(legacy) || Path.GetFullPath(legacy) == Path.GetFullPath(databasePath)) return false;

        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = legacy }.ToString()))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "VACUUM INTO $target;";
            command.Parameters.AddWithValue("$target", databasePath);
            command.ExecuteNonQuery();
        }
        SqliteConnection.ClearAllPools();

        Console.WriteLine($"Base de datos copiada de {LegacyFileName} a {Path.GetFileName(databasePath)}. La original queda como respaldo.");
        return true;
    }
}

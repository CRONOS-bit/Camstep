namespace Camstep.Services;

public sealed class AppPaths
{
    public AppPaths(string dataRoot)
    {
        DataRoot = dataRoot;
        Directory.CreateDirectory(DataRoot);
    }

    public string DataRoot { get; }
    public string AppData => Path.Combine(DataRoot, "App_Data");
    public string Uploads => Path.Combine(AppData, "uploads");
    public string Keys => Path.Combine(AppData, "keys");
    public string Backups => Path.Combine(DataRoot, "backups");

    public static AppPaths From(IConfiguration configuration, string contentRoot)
    {
        var configured = configuration["DataRoot"];
        var root = string.IsNullOrWhiteSpace(configured) ? contentRoot : Path.GetFullPath(configured, contentRoot);
        return new AppPaths(root);
    }
}

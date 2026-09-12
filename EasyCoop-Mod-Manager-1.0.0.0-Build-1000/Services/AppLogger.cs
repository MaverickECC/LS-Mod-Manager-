namespace EasyCoop.ModManager.Services;

public static class AppLogger
{
    private static readonly object Gate = new();
    public static string LogFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EasyCoop", "Mod Manager", "logs");

    public static void Information(string message) => Write("INFO", message);
    public static void Error(Exception exception, string context) => Write("ERROR", $"{context}: {exception}");
    public static void Error(string message, string context) => Write("ERROR", $"{context}: {message}");

    private static void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            var path = Path.Combine(LogFolder, $"easycoop-mod-manager-{DateTime.UtcNow:yyyy-MM-dd}.log");
            lock (Gate)
                File.AppendAllText(path, $"{DateTime.UtcNow:O} [{level}] {message}{Environment.NewLine}");
        }
        catch { }
    }
}

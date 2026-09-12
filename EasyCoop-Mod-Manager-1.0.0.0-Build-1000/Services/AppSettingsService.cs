using System.Text.Json;
using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public sealed class AppSettingsService
{
    private readonly string _path;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    public string SettingsPath => _path;

    public AppSettingsService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(localAppData,
            "EasyCoop", "Mod Manager");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "settings.json");
        ImportLegacyFile(Path.Combine(localAppData, "FieldForge", "Mod Manager", "settings.json"), _path);
    }

    private static void ImportLegacyFile(string legacyPath, string destination)
    {
        if (!File.Exists(destination) && File.Exists(legacyPath))
            File.Copy(legacyPath, destination, false);
    }

    public AppSettings Load()
    {
        try
        {
            var settings = File.Exists(_path)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), _options) ?? new AppSettings()
                : new AppSettings();
            settings.CustomModPaths ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            settings.ModFolders ??= new Dictionary<string, List<ModFolderDefinition>>(StringComparer.OrdinalIgnoreCase);
            settings.ActiveModFolderIds ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            settings.GameLaunchSettings ??= new Dictionary<string, GameLaunchSettings>(StringComparer.OrdinalIgnoreCase);
            settings.Preferences ??= new UserPreferences();
            return settings;
        }
        catch (JsonException) { return new AppSettings(); }
    }

    public void Save(AppSettings settings)
    {
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, _options));
        File.Move(temporary, _path, true);
    }
}

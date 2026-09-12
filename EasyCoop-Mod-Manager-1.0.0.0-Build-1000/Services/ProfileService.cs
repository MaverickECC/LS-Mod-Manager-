using System.Text.Json;
using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public sealed class ProfileService
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public ProfileService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataFolder = Path.Combine(
            localAppData,
            "EasyCoop", "Mod Manager");
        Directory.CreateDirectory(dataFolder);
        _filePath = Path.Combine(dataFolder, "profiles.json");
        var legacyPath = Path.Combine(localAppData, "FieldForge", "Mod Manager", "profiles.json");
        if (!File.Exists(_filePath) && File.Exists(legacyPath)) File.Copy(legacyPath, _filePath, false);
    }

    public IReadOnlyList<ModProfile> Load()
    {
        if (!File.Exists(_filePath)) return Array.Empty<ModProfile>();
        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ModProfile>>(json, _jsonOptions) ?? new List<ModProfile>();
        }
        catch (JsonException)
        {
            BackupInvalidFile();
            return Array.Empty<ModProfile>();
        }
    }

    public void Save(IReadOnlyCollection<ModProfile> profiles)
    {
        var temporaryPath = _filePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(profiles, _jsonOptions));
        File.Move(temporaryPath, _filePath, true);
    }

    private void BackupInvalidFile()
    {
        var backup = _filePath + $".invalid-{DateTime.Now:yyyyMMdd-HHmmss}.bak";
        File.Copy(_filePath, backup, true);
    }
}

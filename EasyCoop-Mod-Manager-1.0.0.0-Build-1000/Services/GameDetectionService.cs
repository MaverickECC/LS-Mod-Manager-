using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public static class GameDetectionService
{
    public static IReadOnlyList<GameInstallation> Detect(AppSettings? settings = null)
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var myGames = Path.Combine(documents, "My Games");

        return new[]
        {
            Create("Landwirtschafts-Simulator 19", "LS19", myGames, "FarmingSimulator2019", settings),
            Create("Landwirtschafts-Simulator 22", "LS22", myGames, "FarmingSimulator2022", settings),
            Create("Landwirtschafts-Simulator 25", "LS25", myGames, "FarmingSimulator2025", settings)
        };
    }

    private static GameInstallation Create(string name, string shortName, string root, string folder, AppSettings? settings)
    {
        var defaultPath = Path.Combine(root, folder, "mods");
        var active = settings is null ? null : ModFolderService.GetFolders(settings, shortName, defaultPath)
            .FirstOrDefault(item => settings.ActiveModFolderIds.TryGetValue(shortName, out var id) && item.Id == id);
        if (active is null && settings is not null && settings.CustomModPaths.TryGetValue(shortName, out var legacyPath)
            && !string.IsNullOrWhiteSpace(legacyPath))
            active = new ModFolderDefinition { Name = "Eigener Modordner", Path = legacyPath };
        return new GameInstallation
        {
            Name = name,
            ShortName = shortName,
            ModsPath = Path.GetFullPath(active?.Path ?? defaultPath),
            IsCustomPath = active is not null && !active.IsDefault
        };
    }
}

using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public static class ModFolderService
{
    public static List<ModFolderDefinition> GetFolders(AppSettings settings, string game, string defaultPath)
    {
        if (!settings.ModFolders.TryGetValue(game, out var folders))
        {
            folders = new List<ModFolderDefinition>();
            settings.ModFolders[game] = folders;
        }

        var standard = folders.FirstOrDefault(f => f.IsDefault);
        if (standard is null)
        {
            standard = new ModFolderDefinition { Id = $"default-{game.ToLowerInvariant()}", Name = "Standard-Modordner", Path = defaultPath, IsDefault = true };
            folders.Insert(0, standard);
        }
        else standard.Path = defaultPath;

        if (settings.CustomModPaths.TryGetValue(game, out var legacy) && !string.IsNullOrWhiteSpace(legacy)
            && !folders.Any(f => string.Equals(Path.GetFullPath(f.Path), Path.GetFullPath(legacy), StringComparison.OrdinalIgnoreCase)))
            folders.Add(new ModFolderDefinition { Name = "Bisheriger Modordner", Path = Path.GetFullPath(legacy) });

        if (!settings.ActiveModFolderIds.ContainsKey(game))
            settings.ActiveModFolderIds[game] = folders.FirstOrDefault(f => !f.IsDefault)?.Id ?? standard.Id;
        return folders;
    }
}

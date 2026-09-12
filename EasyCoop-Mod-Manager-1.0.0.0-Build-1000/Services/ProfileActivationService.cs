using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public sealed class ProfileActivationService
{
    private readonly ModOperationService _operations;

    public ProfileActivationService(ModOperationService operations) => _operations = operations;

    public ProfileActivationResult Activate(ModProfile profile, IEnumerable<ModInfo> mods, string modsPath)
    {
        var desired = profile.ModFileNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allMods = mods.ToList();
        var disabledCount = 0;
        var activatedCount = 0;

        foreach (var mod in allMods.Where(m => m.StorageState == ModStorageState.Active && !desired.Contains(m.FileName)))
        {
            _operations.Disable(mod, modsPath);
            disabledCount++;
        }

        foreach (var mod in allMods.Where(m => m.StorageState == ModStorageState.Disabled && desired.Contains(m.FileName)))
        {
            _operations.Activate(mod, modsPath);
            activatedCount++;
        }

        var available = allMods
            .Where(m => m.StorageState != ModStorageState.Trash)
            .Select(m => m.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = desired.Where(name => !available.Contains(name)).OrderBy(name => name).ToList();
        return new ProfileActivationResult(activatedCount, disabledCount, missing);
    }
}

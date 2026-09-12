using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public sealed class ModAnalysisService
{
    public void Analyze(IReadOnlyList<ModInfo> mods, string gameShortName, UserPreferences? preferences = null)
    {
        preferences ??= new UserPreferences();
        foreach (var mod in mods)
        {
            mod.Issues.Clear();
            AnalyzeStructure(mod);
            AnalyzeGameVersion(mod, gameShortName);
        }

        if (preferences.DetectDuplicatesAndDamage && preferences.AnalysisDepth != "Schnell") AnalyzeDuplicates(mods);
        if (preferences.AnalysisDepth == "Tief") AnalyzeTitleConflicts(mods);
        if (preferences.CheckDependenciesAndConflicts && preferences.AnalysisDepth != "Schnell") AnalyzeDependencies(mods);
    }

    private static void AnalyzeStructure(ModInfo mod)
    {
        if (!string.Equals(mod.Status, "OK", StringComparison.OrdinalIgnoreCase))
            Add(mod, ModIssueSeverity.Error, mod.Status);
        if (mod.DescVersion is null && mod.Status == "OK")
            Add(mod, ModIssueSeverity.Warning, "Keine gültige descVersion in modDesc.xml gefunden.");
    }

    private static void AnalyzeGameVersion(ModInfo mod, string gameShortName)
    {
        if (mod.DescVersion is int version && !MatchesGame(version, gameShortName))
            Add(mod, ModIssueSeverity.Error,
                $"descVersion {version} passt nicht zum erwarteten Bereich für {gameShortName}.");

        var prefix = Path.GetFileNameWithoutExtension(mod.FileName);
        var otherPrefix = new[] { "FS19_", "FS22_", "FS25_" }
            .FirstOrDefault(p => prefix.StartsWith(p, StringComparison.OrdinalIgnoreCase) &&
                                 !p.StartsWith(gameShortName.Replace("LS", "FS"), StringComparison.OrdinalIgnoreCase));
        if (otherPrefix is not null)
            Add(mod, ModIssueSeverity.Error, $"Der Dateiname kennzeichnet die Mod als {otherPrefix[..4].Replace("FS", "LS")}.");
    }

    private static bool MatchesGame(int descVersion, string gameShortName) => gameShortName switch
    {
        "LS19" => descVersion is >= 30 and < 60,
        "LS22" => descVersion is >= 60 and < 90,
        "LS25" => descVersion is >= 90 and < 130,
        _ => true
    };

    private static void AnalyzeDuplicates(IReadOnlyList<ModInfo> mods)
    {
        foreach (var group in mods.Where(m => m.Sha256.Length > 0).GroupBy(m => m.Sha256).Where(g => g.Count() > 1))
        {
            var files = string.Join(", ", group.Select(m => m.FileName).Distinct(StringComparer.OrdinalIgnoreCase));
            foreach (var mod in group)
                Add(mod, ModIssueSeverity.Warning, $"Identischer Dateiinhalt mehrfach vorhanden: {files}.");
        }
    }

    private static void AnalyzeTitleConflicts(IReadOnlyList<ModInfo> mods)
    {
        var active = mods.Where(m => m.StorageState == ModStorageState.Active && m.Status == "OK");
        foreach (var group in active.GroupBy(m => m.Title, StringComparer.CurrentCultureIgnoreCase)
                     .Where(g => g.Select(m => m.Sha256).Distinct().Count() > 1))
        {
            foreach (var mod in group)
                Add(mod, ModIssueSeverity.Warning,
                    "Mehrere aktive Mods verwenden denselben Titel, besitzen aber unterschiedliche Inhalte.");
        }
    }

    private static void AnalyzeDependencies(IReadOnlyList<ModInfo> mods)
    {
        var activeNames = mods.Where(m => m.StorageState == ModStorageState.Active)
            .Select(m => Path.GetFileNameWithoutExtension(m.FileName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in mods.Where(m => m.StorageState == ModStorageState.Active))
        {
            foreach (var dependency in mod.Dependencies)
            {
                var normalized = Path.GetFileNameWithoutExtension(dependency.Trim());
                if (!activeNames.Contains(normalized))
                    Add(mod, ModIssueSeverity.Error, $"Benötigte Mod fehlt oder ist deaktiviert: {dependency}.");
            }
        }
    }

    private static void Add(ModInfo mod, ModIssueSeverity severity, string message)
    {
        if (!mod.Issues.Any(i => i.Severity == severity && i.Message == message))
            mod.Issues.Add(new ModIssue(severity, message));
    }
}

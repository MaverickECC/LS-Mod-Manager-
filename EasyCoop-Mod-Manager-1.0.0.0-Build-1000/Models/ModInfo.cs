namespace EasyCoop.ModManager.Models;

public sealed class ModInfo
{
    public required string FileName { get; init; }
    public required string FullPath { get; init; }
    public string Title { get; init; } = "Unbekannte Mod";
    public string Version { get; init; } = "–";
    public string Author { get; init; } = "–";
    public string Description { get; init; } = "";
    public System.Windows.Media.ImageSource? IconImage { get; init; }
    public string Status { get; init; } = "OK";
    public bool IsRecognized { get; init; }
    public int? DescVersion { get; init; }
    public string DescVersionText => DescVersion?.ToString() ?? "–";
    public string Sha256 { get; init; } = "";
    public List<string> Dependencies { get; init; } = new();
    public List<ModIssue> Issues { get; } = new();
    public string AnalysisStatus => Issues.Any(i => i.Severity == ModIssueSeverity.Error) ? "Fehler"
        : Issues.Any(i => i.Severity == ModIssueSeverity.Warning) ? "Warnung"
        : "OK";
    public string IssuesText => Issues.Count == 0
        ? "Keine Auffälligkeiten gefunden."
        : string.Join(Environment.NewLine, Issues.Select(i => $"{SeverityText(i.Severity)}: {i.Message}"));
    public ModStorageState StorageState { get; init; } = ModStorageState.Active;
    public string StorageStateText => StorageState switch
    {
        ModStorageState.Active => "Aktiv",
        ModStorageState.Disabled => "Deaktiviert",
        ModStorageState.Trash => "Papierkorb",
        _ => "Unbekannt"
    };
    public long SizeBytes { get; init; }
    public string SizeText => SizeBytes < 1024 * 1024
        ? $"{SizeBytes / 1024d:0.0} KB"
        : $"{SizeBytes / 1024d / 1024d:0.0} MB";

    private static string SeverityText(ModIssueSeverity severity) => severity switch
    {
        ModIssueSeverity.Error => "FEHLER",
        ModIssueSeverity.Warning => "WARNUNG",
        _ => "HINWEIS"
    };
}

public enum ModStorageState
{
    Active,
    Disabled,
    Trash
}

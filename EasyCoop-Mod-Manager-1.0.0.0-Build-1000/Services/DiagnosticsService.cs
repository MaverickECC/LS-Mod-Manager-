using System.IO.Compression;
using System.Text.Json;
using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public sealed class DiagnosticsService
{
    public void Export(string destination, IReadOnlyList<GameInstallation> games, IReadOnlyList<ModInfo> currentMods)
    {
        using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(output, ZipArchiveMode.Create);
        var reportEntry = archive.CreateEntry("diagnose.json", CompressionLevel.Optimal);
        using (var writer = new StreamWriter(reportEntry.Open()))
        {
            var report = new
            {
                Application = "EasyCoop Mod Manager",
                Version = BuildInfo.Version,
                CreatedUtc = DateTime.UtcNow,
                OperatingSystem = Environment.OSVersion.VersionString,
                Runtime = Environment.Version.ToString(),
                Games = games.Select(g => new { g.Name, g.ShortName, g.ModsPath, g.IsAvailable, g.IsCustomPath }),
                Mods = currentMods.Select(m => new
                {
                    m.FileName, m.Title, m.Version, m.Author, m.DescVersion,
                    m.StorageStateText, m.AnalysisStatus,
                    Issues = m.Issues.Select(i => new { Severity = i.Severity.ToString(), i.Message })
                })
            };
            writer.Write(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        }

        if (!Directory.Exists(AppLogger.LogFolder)) return;
        foreach (var log in Directory.EnumerateFiles(AppLogger.LogFolder, "*.log").OrderByDescending(File.GetLastWriteTimeUtc).Take(3))
            archive.CreateEntryFromFile(log, Path.Combine("logs", Path.GetFileName(log)), CompressionLevel.Optimal);
    }
}

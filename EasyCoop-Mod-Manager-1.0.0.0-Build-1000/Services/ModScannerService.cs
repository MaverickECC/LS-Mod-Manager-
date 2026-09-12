using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using System.Security.Cryptography;
using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public sealed class ModScannerService
{
    public async Task<IReadOnlyList<ModInfo>> ScanAsync(string modsPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(modsPath)) return Array.Empty<ModInfo>();

        return await Task.Run(() =>
        {
            var results = new List<ModInfo>();
            foreach (var (folder, state) in EnumerateStorageFolders(modsPath))
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var file in Directory.EnumerateFiles(folder, "*.zip", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    results.Add(ReadMod(file, state));
                }
            }

            return (IReadOnlyList<ModInfo>)results
                .OrderBy(m => m.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }, cancellationToken);
    }

    private static IEnumerable<(string Folder, ModStorageState State)> EnumerateStorageFolders(string modsPath)
    {
        yield return (modsPath, ModStorageState.Active);
        yield return (Path.Combine(modsPath, ".easycoop-disabled"), ModStorageState.Disabled);
        yield return (Path.Combine(modsPath, ".easycoop-trash"), ModStorageState.Trash);
        // Verwaltungsordner älterer FieldForge-Versionen bleiben nach dem Upgrade erreichbar.
        yield return (Path.Combine(modsPath, ".fieldforge-disabled"), ModStorageState.Disabled);
        yield return (Path.Combine(modsPath, ".fieldforge-trash"), ModStorageState.Trash);
    }

    private static ModInfo ReadMod(string path, ModStorageState state)
    {
        var info = new FileInfo(path);
        try
        {
            using var archive = ZipFile.OpenRead(path);
            var entry = archive.Entries.FirstOrDefault(e =>
                string.Equals(e.FullName, "modDesc.xml", StringComparison.OrdinalIgnoreCase));

            if (entry is null) return Invalid(info, "modDesc.xml fehlt", state);

            using var stream = entry.Open();
            var xml = XDocument.Load(stream, LoadOptions.None);
            var root = xml.Root;
            if (root is null) return Invalid(info, "Ungültige XML-Datei", state);

            System.Windows.Media.ImageSource? icon = null;
            try { icon = ModIconService.Read(archive, root.Element("iconFilename")?.Value.Trim()); }
            catch { /* Eine defekte Vorschau darf das Einlesen der Mod nicht verhindern. */ }

            return new ModInfo
            {
                FileName = info.Name,
                FullPath = info.FullName,
                SizeBytes = info.Length,
                Title = LocalizedValue(root.Element("title")) ?? Path.GetFileNameWithoutExtension(path),
                Version = root.Element("version")?.Value.Trim() ?? "–",
                Author = root.Element("author")?.Value.Trim() ?? "–",
                Description = LocalizedValue(root.Element("description")) ?? "",
                IconImage = icon,
                DescVersion = int.TryParse(root.Attribute("descVersion")?.Value, out var descVersion) ? descVersion : null,
                Dependencies = ReadDependencies(root),
                Sha256 = ComputeSha256(path),
                Status = "OK",
                IsRecognized = true,
                StorageState = state
            };
        }
        catch (InvalidDataException) { return Invalid(info, "ZIP beschädigt", state); }
        catch (Exception ex) { return Invalid(info, $"Fehler: {ex.Message}", state); }
    }

    private static string? LocalizedValue(XElement? element)
    {
        if (element is null) return null;
        var language = element.Element("de") ?? element.Element("en");
        var value = (language?.Value ?? element.Value).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static List<string> ReadDependencies(XElement root) => root
        .Element("dependencies")?
        .Elements("dependency")
        .Select(e => e.Value.Trim())
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList() ?? new List<string>();

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static ModInfo Invalid(FileInfo info, string status, ModStorageState state) => new()
    {
        FileName = info.Name,
        FullPath = info.FullName,
        SizeBytes = info.Length,
        Title = Path.GetFileNameWithoutExtension(info.Name),
        Status = status,
        StorageState = state
    };
}

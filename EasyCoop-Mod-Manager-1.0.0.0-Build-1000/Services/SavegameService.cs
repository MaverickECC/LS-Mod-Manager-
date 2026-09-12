using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public sealed class SavegameService
{
    public string BackupRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EasyCoop", "Mod Manager", "savegame-backups");

    public string GetGameUserFolder(string gameShortName)
    {
        var folder = gameShortName switch
        {
            "LS19" => "FarmingSimulator2019",
            "LS22" => "FarmingSimulator2022",
            _ => "FarmingSimulator2025"
        };
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", folder);
    }

    public IReadOnlyList<SavegameInfo> Scan(string gameShortName)
    {
        var root = GetGameUserFolder(gameShortName);
        if (!Directory.Exists(root)) return Array.Empty<SavegameInfo>();

        return Directory.EnumerateDirectories(root, "savegame*")
            .Where(path => int.TryParse(Path.GetFileName(path)[8..], out _))
            .Select(ReadSavegame)
            .OrderBy(item => int.Parse(item.SlotName[8..]))
            .ToList();
    }

    public IReadOnlyList<SavegameBackupInfo> GetBackups(string gameShortName, string slotName)
    {
        var root = Path.Combine(BackupRoot, gameShortName, slotName);
        if (!Directory.Exists(root)) return Array.Empty<SavegameBackupInfo>();
        return Directory.EnumerateDirectories(root)
            .Select(path => new SavegameBackupInfo
            {
                Name = Path.GetFileName(path),
                FolderPath = path,
                Created = Directory.GetCreationTime(path),
                SizeBytes = GetDirectorySize(path)
            })
            .OrderByDescending(item => item.Created)
            .ToList();
    }

    public string CreateBackup(string gameShortName, SavegameInfo savegame, string reason = "manual")
    {
        EnsureGameIsNotRunning(gameShortName);
        if (!Directory.Exists(savegame.FolderPath)) throw new DirectoryNotFoundException("Der Spielstandordner wurde nicht gefunden.");
        var safeReason = string.Concat(reason.Where(char.IsLetterOrDigit));
        var target = Path.Combine(BackupRoot, gameShortName, savegame.SlotName,
            $"{DateTime.Now:yyyyMMdd-HHmmss}-{safeReason}");
        CopyDirectory(savegame.FolderPath, target);
        AppLogger.Information($"Savegame-Backup erstellt: {gameShortName}/{savegame.SlotName} -> {target}");
        return target;
    }

    public void Restore(string gameShortName, SavegameInfo savegame, SavegameBackupInfo backup)
    {
        EnsureGameIsNotRunning(gameShortName);
        if (!Directory.Exists(backup.FolderPath)) throw new DirectoryNotFoundException("Das ausgewählte Backup wurde nicht gefunden.");

        CreateBackup(gameShortName, savegame, "vor-wiederherstellung");
        var parent = Directory.GetParent(savegame.FolderPath)?.FullName
            ?? throw new InvalidOperationException("Ungültiger Spielstandpfad.");
        var stage = Path.Combine(parent, $".easycoop-restore-{Guid.NewGuid():N}");
        var old = Path.Combine(parent, $".easycoop-old-{Guid.NewGuid():N}");
        CopyDirectory(backup.FolderPath, stage);
        try
        {
            Directory.Move(savegame.FolderPath, old);
            try
            {
                Directory.Move(stage, savegame.FolderPath);
            }
            catch
            {
                if (!Directory.Exists(savegame.FolderPath) && Directory.Exists(old)) Directory.Move(old, savegame.FolderPath);
                throw;
            }

            try { Directory.Delete(old, true); }
            catch (Exception exception) { AppLogger.Error(exception, "Temporären alten Spielstand entfernen"); }
            AppLogger.Information($"Savegame wiederhergestellt: {gameShortName}/{savegame.SlotName} aus {backup.Name}");
        }
        finally
        {
            try { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
            catch (Exception exception) { AppLogger.Error(exception, "Temporäre Wiederherstellungsdateien entfernen"); }
        }
    }

    public void UpdateBasicValues(string gameShortName, SavegameInfo savegame, SavegameBasicValues values)
    {
        EnsureGameIsNotRunning(gameShortName);
        if (values.Money < -999_999_999m || values.Money > 999_999_999_999m)
            throw new ArgumentOutOfRangeException(nameof(values.Money), "Der Geldwert liegt außerhalb des erlaubten Bereichs.");
        var savegameName = ValidateName(values.SavegameName, "Spielstandname");
        var farmName = values.FarmName.Trim();
        if (farmName.Length > 80 || farmName.Any(char.IsControl))
            throw new ArgumentException("Der Hofname ist ungültig oder länger als 80 Zeichen.");

        var careerPath = Path.Combine(savegame.FolderPath, "careerSavegame.xml");
        var farmsPath = Path.Combine(savegame.FolderPath, "farms.xml");
        if (!File.Exists(careerPath) || !File.Exists(farmsPath))
            throw new InvalidDataException("careerSavegame.xml oder farms.xml fehlt. Der Spielstand wird nicht verändert.");

        var career = XDocument.Load(careerPath, LoadOptions.PreserveWhitespace);
        var farms = XDocument.Load(farmsPath, LoadOptions.PreserveWhitespace);
        var nameElement = FindElement(career, "savegameName")
            ?? throw new InvalidDataException("Der Spielstandname wurde in careerSavegame.xml nicht eindeutig erkannt.");
        var careerMoney = FindCareerMoney(career)
            ?? throw new InvalidDataException("Der Geldwert wurde in careerSavegame.xml nicht erkannt.");
        var farm = FindPrimaryFarm(farms)
            ?? throw new InvalidDataException("Der Haupthof wurde in farms.xml nicht erkannt.");
        var moneyAttribute = FindAttribute(farm, "money")
            ?? throw new InvalidDataException("Der Geldwert wurde in farms.xml nicht erkannt.");

        CreateBackup(gameShortName, savegame, "vor-bearbeitung");
        var moneyText = values.Money.ToString("0.##", CultureInfo.InvariantCulture);
        nameElement.Value = savegameName;
        careerMoney.Value = moneyText;
        moneyAttribute.Value = moneyText;
        if (!string.IsNullOrWhiteSpace(farmName))
        {
            var farmNameAttribute = FindAttribute(farm, "name")
                ?? throw new InvalidDataException("Der Hofname wird von diesem Spielstand nicht unterstützt.");
            farmNameAttribute.Value = farmName;
        }

        WriteXmlAtomically(careerPath, career);
        try { WriteXmlAtomically(farmsPath, farms); }
        catch
        {
            var latest = GetBackups(gameShortName, savegame.SlotName).FirstOrDefault(item => item.Name.Contains("vorbearbeitung", StringComparison.OrdinalIgnoreCase));
            if (latest is not null) RestoreFilesOnly(latest.FolderPath, savegame.FolderPath);
            throw;
        }
        AppLogger.Information($"Savegame-Grundwerte gespeichert: {gameShortName}/{savegame.SlotName}");
    }

    public void EnsureGameIsNotRunning(string gameShortName)
    {
        var processName = gameShortName switch
        {
            "LS19" => "FarmingSimulator2019Game",
            "LS22" => "FarmingSimulator2022Game",
            _ => "FarmingSimulator2025Game"
        };
        var alternate = processName.Replace("Game", "");
        if (Process.GetProcessesByName(processName).Length > 0 || Process.GetProcessesByName(alternate).Length > 0)
            throw new InvalidOperationException($"{gameShortName} läuft noch. Bitte beende das Spiel vor Sicherung oder Wiederherstellung.");
    }

    private static SavegameInfo ReadSavegame(string path)
    {
        var slot = Path.GetFileName(path);
        var name = slot;
        var map = "Nicht erkannt";
        decimal? money = null;
        var farmName = "";
        var canEditName = false;
        var canEditMoney = false;
        var canEditFarmName = false;
        var career = Path.Combine(path, "careerSavegame.xml");
        var farmsPath = Path.Combine(path, "farms.xml");
        try
        {
            if (File.Exists(career))
            {
                var document = XDocument.Load(career, LoadOptions.None);
                name = FindValue(document, "savegameName") ?? name;
                map = FindValue(document, "mapTitle") ?? FindValue(document, "mapId") ?? map;
                canEditName = FindElement(document, "savegameName") is not null;
                var careerMoney = FindCareerMoney(document)?.Value;
                if (decimal.TryParse(careerMoney, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)) money = parsed;
            }
            if (File.Exists(farmsPath))
            {
                var farms = XDocument.Load(farmsPath, LoadOptions.None);
                var farm = FindPrimaryFarm(farms);
                var moneyValue = farm is null ? null : FindAttribute(farm, "money")?.Value;
                if (decimal.TryParse(moneyValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)) money = parsed;
                farmName = farm is null ? "" : FindAttribute(farm, "name")?.Value ?? "";
                canEditMoney = farm is not null && FindAttribute(farm, "money") is not null && File.Exists(career);
                canEditFarmName = farm is not null && FindAttribute(farm, "name") is not null;
            }
        }
        catch (Exception exception) { AppLogger.Error(exception, $"Savegame-Metadaten {slot}"); }

        return new SavegameInfo
        {
            SlotName = slot,
            DisplayName = name,
            MapName = map,
            FolderPath = path,
            LastModified = Directory.GetLastWriteTime(path),
            SizeBytes = GetDirectorySize(path),
            Money = money,
            FarmName = farmName,
            CanEditMoney = canEditMoney,
            CanEditSavegameName = canEditName,
            CanEditFarmName = canEditFarmName
        };
    }

    private static XElement? FindElement(XDocument document, string localName) => document.Descendants()
        .FirstOrDefault(item => item.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));

    private static XElement? FindCareerMoney(XDocument document) => document.Descendants()
        .Where(item => item.Name.LocalName.Equals("statistics", StringComparison.OrdinalIgnoreCase))
        .SelectMany(item => item.Elements())
        .FirstOrDefault(item => item.Name.LocalName.Equals("money", StringComparison.OrdinalIgnoreCase));

    private static XElement? FindPrimaryFarm(XDocument document) => document.Descendants()
        .Where(item => item.Name.LocalName.Equals("farm", StringComparison.OrdinalIgnoreCase))
        .FirstOrDefault(item => FindAttribute(item, "money") is not null && FindAttribute(item, "farmId")?.Value != "0");

    private static XAttribute? FindAttribute(XElement element, string localName) => element.Attributes()
        .FirstOrDefault(item => item.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));

    private static string? FindValue(XDocument document, string localName)
    {
        var element = document.Descendants().FirstOrDefault(item => item.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(element?.Value) ? null : element.Value.Trim();
    }

    private static string ValidateName(string value, string label)
    {
        value = value.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 80 || value.Any(char.IsControl))
            throw new ArgumentException($"{label} darf nicht leer, länger als 80 Zeichen oder mit Steuerzeichen versehen sein.");
        return value;
    }

    private static void WriteXmlAtomically(string destination, XDocument document)
    {
        var temporary = destination + $".easycoop-{Guid.NewGuid():N}.tmp";
        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false, NewLineHandling = NewLineHandling.None };
        try
        {
            using (var writer = XmlWriter.Create(temporary, settings)) document.Save(writer);
            _ = XDocument.Load(temporary, LoadOptions.None);
            File.Move(temporary, destination, true);
        }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
    }

    private static void RestoreFilesOnly(string backupFolder, string destination)
    {
        foreach (var fileName in new[] { "careerSavegame.xml", "farms.xml" })
        {
            var source = Path.Combine(backupFolder, fileName);
            if (File.Exists(source)) File.Copy(source, Path.Combine(destination, fileName), true);
        }
    }

    private static long GetDirectorySize(string path) => Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
        .Sum(file => { try { return new FileInfo(file).Length; } catch { return 0; } });

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }
}

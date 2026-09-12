using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public sealed class AdvancedSavegameService
{
    private static readonly Regex SegmentPattern = new("^(?<name>.+)\\[(?<index>\\d+)\\]$", RegexOptions.Compiled);
    private readonly SavegameService _savegames;
    public AdvancedSavegameService(SavegameService savegames) => _savegames = savegames;

    public IReadOnlyList<AdvancedSavegameValue> Scan(SavegameInfo savegame)
    {
        var result = new List<AdvancedSavegameValue>();
        ScanVehicles(Path.Combine(savegame.FolderPath, "vehicles.xml"), result);
        ScanAnimals(Path.Combine(savegame.FolderPath, "animals.xml"), result);
        ScanAnimals(Path.Combine(savegame.FolderPath, "placeables.xml"), result);
        ScanFarmlands(Path.Combine(savegame.FolderPath, "farmland.xml"), result);
        ScanProductions(Path.Combine(savegame.FolderPath, "placeables.xml"), result);
        return result.OrderBy(item => item.Category).ThenBy(item => item.ObjectName).ThenBy(item => item.PropertyName).ToList();
    }

    public void Save(string gameShortName, SavegameInfo savegame, IReadOnlyList<AdvancedSavegameValue> values)
    {
        var changed = values.Where(item => item.IsModified).ToList();
        if (changed.Count == 0) return;
        _savegames.EnsureGameIsNotRunning(gameShortName);
        foreach (var item in changed) Validate(item);
        var backup = _savegames.CreateBackup(gameShortName, savegame, "vor-erweitert");
        var written = new List<string>();
        try
        {
            foreach (var group in changed.GroupBy(item => item.FilePath, StringComparer.OrdinalIgnoreCase))
            {
                var document = XDocument.Load(group.Key, LoadOptions.PreserveWhitespace);
                foreach (var item in group)
                {
                    var attribute = ResolveAttribute(document, item.Locator)
                        ?? throw new InvalidDataException($"Eintrag nicht mehr gefunden: {item.Locator}");
                    attribute.Value = Normalize(item);
                }
                WriteXmlAtomically(group.Key, document);
                written.Add(group.Key);
            }
            AppLogger.Information($"Erweiterte Savegame-Werte gespeichert: {gameShortName}/{savegame.SlotName}, {changed.Count} Änderungen");
        }
        catch
        {
            foreach (var file in written)
            {
                var source = Path.Combine(backup, Path.GetFileName(file));
                if (File.Exists(source)) File.Copy(source, file, true);
            }
            throw;
        }
    }

    private static void ScanVehicles(string path, List<AdvancedSavegameValue> result)
    {
        if (!File.Exists(path)) return;
        var document = SafeLoad(path);
        if (document is null) return;
        foreach (var element in document.Descendants())
        {
            var vehicle = element.AncestorsAndSelf().FirstOrDefault(x => x.Name.LocalName.Equals("vehicle", StringComparison.OrdinalIgnoreCase));
            if (vehicle is null) continue;
            var name = Path.GetFileNameWithoutExtension(Attr(vehicle, "filename")?.Value ?? Attr(vehicle, "id")?.Value ?? "Fahrzeug");
            AddNumeric(result, "Fahrzeuge", name, element, path, "damage", "Schaden", 0, 1);
            AddNumeric(result, "Fahrzeuge", name, element, path, "wear", "Verschleiß", 0, 1);
            AddNumeric(result, "Fahrzeuge", name, element, path, "dirtAmount", "Verschmutzung", 0, 1);
            AddNumeric(result, "Fahrzeuge", name, element, path, "operatingTime", "Betriebszeit", 0, null);
            AddNumeric(result, "Fahrzeuge", name, element, path, "fillLevel", "Füllstand", 0, null);
        }
    }

    private static void ScanAnimals(string path, List<AdvancedSavegameValue> result)
    {
        if (!File.Exists(path)) return;
        var document = SafeLoad(path);
        if (document is null) return;
        foreach (var element in document.Descendants().Where(x => x.Name.LocalName.Contains("animal", StringComparison.OrdinalIgnoreCase) || x.Name.LocalName.Contains("cluster", StringComparison.OrdinalIgnoreCase)))
        {
            var name = Attr(element, "subType")?.Value ?? Attr(element, "type")?.Value ?? "Tiergruppe";
            AddNumeric(result, "Tiere", name, element, path, "numAnimals", "Anzahl", 0, 100000, "integer");
            AddNumeric(result, "Tiere", name, element, path, "count", "Anzahl", 0, 100000, "integer");
            AddNumeric(result, "Tiere", name, element, path, "health", "Gesundheit", 0, 1);
            AddNumeric(result, "Tiere", name, element, path, "reproduction", "Fortpflanzung", 0, 1);
        }
    }

    private static void ScanFarmlands(string path, List<AdvancedSavegameValue> result)
    {
        if (!File.Exists(path)) return;
        var document = SafeLoad(path);
        if (document is null) return;
        foreach (var element in document.Descendants().Where(x => x.Name.LocalName.Equals("farmland", StringComparison.OrdinalIgnoreCase)))
            AddNumeric(result, "Felder", $"Feld {Attr(element, "id")?.Value ?? "?"}", element, path, "farmId", "Besitzer-Farm-ID", 0, 255, "integer");
    }

    private static void ScanProductions(string path, List<AdvancedSavegameValue> result)
    {
        if (!File.Exists(path)) return;
        var document = SafeLoad(path);
        if (document is null) return;
        foreach (var element in document.Descendants().Where(x => x.Ancestors().Any(a => a.Name.LocalName.Contains("production", StringComparison.OrdinalIgnoreCase) || a.Name.LocalName.Equals("storage", StringComparison.OrdinalIgnoreCase))))
        {
            var placeable = element.AncestorsAndSelf().FirstOrDefault(x => x.Name.LocalName.Equals("placeable", StringComparison.OrdinalIgnoreCase));
            var name = Path.GetFileNameWithoutExtension(Attr(placeable, "filename")?.Value ?? Attr(element, "fillType")?.Value ?? "Produktion");
            AddNumeric(result, "Produktionen", name, element, path, "fillLevel", $"Füllstand {Attr(element, "fillType")?.Value ?? ""}".Trim(), 0, null);
            AddBoolean(result, "Produktionen", name, element, path, "isEnabled", "Aktiv");
        }
    }

    private static void AddNumeric(List<AdvancedSavegameValue> result, string category, string name, XElement element, string file, string attributeName, string property, decimal? min, decimal? max, string type = "decimal")
    {
        var attribute = Attr(element, attributeName); if (attribute is null) return;
        result.Add(NewValue(category, name, property, element, attribute, file, type, min, max));
    }
    private static void AddBoolean(List<AdvancedSavegameValue> result, string category, string name, XElement element, string file, string attributeName, string property)
    {
        var attribute = Attr(element, attributeName); if (attribute is null) return;
        result.Add(NewValue(category, name, property, element, attribute, file, "boolean", null, null));
    }
    private static AdvancedSavegameValue NewValue(string category, string name, string property, XElement element, XAttribute attribute, string file, string type, decimal? min, decimal? max) => new()
    {
        Category = category, ObjectName = name, PropertyName = property, FilePath = file,
        Locator = BuildLocator(element, attribute.Name.LocalName), ValueType = type, Minimum = min, Maximum = max,
        OriginalValue = attribute.Value, Value = attribute.Value
    };

    private static XDocument? SafeLoad(string path) { try { return XDocument.Load(path, LoadOptions.None); } catch (Exception ex) { AppLogger.Error(ex, $"Erweiterte Savegame-Analyse {Path.GetFileName(path)}"); return null; } }
    private static XAttribute? Attr(XElement? element, string name) => element?.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
    private static string BuildLocator(XElement element, string attribute) => "/" + string.Join("/", element.AncestorsAndSelf().Reverse().Select(x => $"{x.Name.LocalName}[{x.ElementsBeforeSelf(x.Name).Count()}]")) + "@" + attribute;

    private static XAttribute? ResolveAttribute(XDocument document, string locator)
    {
        var at = locator.LastIndexOf('@'); if (at < 0 || document.Root is null) return null;
        var segments = locator[1..at].Split('/'); XElement? current = null;
        foreach (var segment in segments)
        {
            var match = SegmentPattern.Match(segment); if (!match.Success) return null;
            var name = match.Groups["name"].Value; var index = int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture);
            current = current is null
                ? (document.Root.Name.LocalName == name && index == 0 ? document.Root : null)
                : current.Elements().Where(x => x.Name.LocalName == name).ElementAtOrDefault(index);
            if (current is null) return null;
        }
        return Attr(current, locator[(at + 1)..]);
    }

    private static void Validate(AdvancedSavegameValue item)
    {
        if (item.ValueType == "boolean")
        {
            if (!bool.TryParse(item.Value, out _)) throw new ArgumentException($"{item.PropertyName}: Erlaubt sind true oder false.");
            return;
        }
        if (!decimal.TryParse(item.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            throw new ArgumentException($"{item.PropertyName}: Ungültige Zahl.");
        if (item.ValueType == "integer" && decimal.Truncate(number) != number)
            throw new ArgumentException($"{item.PropertyName}: Ganze Zahl erforderlich.");
        if (item.Minimum is not null && number < item.Minimum || item.Maximum is not null && number > item.Maximum)
            throw new ArgumentOutOfRangeException(item.PropertyName, $"Wert muss zwischen {item.Minimum ?? decimal.MinValue} und {item.Maximum ?? decimal.MaxValue} liegen.");
    }
    private static string Normalize(AdvancedSavegameValue item) => item.ValueType switch
    {
        "boolean" => bool.Parse(item.Value).ToString().ToLowerInvariant(),
        "integer" => long.Parse(item.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
        _ => decimal.Parse(item.Value, CultureInfo.InvariantCulture).ToString("0.######", CultureInfo.InvariantCulture)
    };
    private static void WriteXmlAtomically(string destination, XDocument document)
    {
        var temp = destination + $".easycoop-{Guid.NewGuid():N}.tmp";
        try { using (var writer = XmlWriter.Create(temp, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false })) document.Save(writer); _ = XDocument.Load(temp); File.Move(temp, destination, true); }
        finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
    }
}

namespace EasyCoop.ModManager.Models;

public sealed class GameInstallation
{
    public required string Name { get; init; }
    public required string ShortName { get; init; }
    public required string ModsPath { get; set; }
    public bool IsCustomPath { get; set; }
    public string LogoSource => ShortName switch
    {
        "LS19" => "Assets/LS19-Official-Logo.png",
        "LS22" => "Assets/LS22-Official-Logo.png",
        _ => "Assets/LS25-Official-Logo.png"
    };
    public bool IsAvailable => Directory.Exists(ModsPath);
    public string Status => IsAvailable
        ? IsCustomPath ? "Eigener Modordner" : "Gefunden"
        : "Nicht gefunden";
}

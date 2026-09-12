namespace EasyCoop.ModManager.Models;

public sealed class ModProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string GameShortName { get; init; }
    public List<string> ModFileNames { get; set; } = new();
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public string Summary => $"{ModFileNames.Count} Mods";
}

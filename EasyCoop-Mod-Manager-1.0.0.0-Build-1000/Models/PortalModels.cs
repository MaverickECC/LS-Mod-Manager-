namespace EasyCoop.ModManager.Models;

public sealed class PortalLoginResponse
{
    public required string Token { get; init; }
    public required PortalUser User { get; init; }
}

public sealed class PortalUser
{
    public required string DisplayName { get; init; }
    public required string Role { get; init; }
}

public sealed class PortalMod
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Version { get; init; }
    public required string Author { get; init; }
    public required string GameVersion { get; init; }
    public required string FileName { get; init; }
    public string Description { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public string? ImageUrl { get; init; }
    public string? PdfUrl { get; init; }
    public bool IsPrivate { get; init; }
    public bool IsFavorite { get; set; }
    public long DownloadCount { get; init; }
    public string AccessText => IsPrivate ? "Privat" : "Öffentlich";
    public string FavoriteText => IsFavorite ? "★" : "☆";
    public string LocalStatus { get; set; } = "Nicht installiert";
}

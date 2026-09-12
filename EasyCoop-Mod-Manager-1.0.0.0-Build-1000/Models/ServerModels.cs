namespace EasyCoop.ModManager.Models;

public sealed class ManagedServer
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string GameVersion { get; init; }
}

public sealed class DedicatedServerStatus
{
    public bool Online { get; init; }
    public string Map { get; init; } = "–";
    public int Players { get; init; }
    public int MaxPlayers { get; init; }
    public string GameVersion { get; init; } = "–";
    public string StatusText => Online ? "Online" : "Offline";
}

public sealed class ServerMod
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string FileName { get; init; }
    public required string Version { get; init; }
    public string Sha256 { get; init; } = "";
}

public sealed class ServerModComparison
{
    public ServerMod? ServerMod { get; init; }
    public ModInfo? LocalMod { get; init; }
    public string Title => ServerMod?.Title ?? LocalMod?.Title ?? "Unbekannt";
    public string FileName => ServerMod?.FileName ?? LocalMod?.FileName ?? "–";
    public string ServerVersion => ServerMod?.Version ?? "–";
    public string LocalVersion => LocalMod?.Version ?? "–";
    public required ServerModState State { get; init; }
    public string StateText => State switch
    {
        ServerModState.Current => "Aktuell",
        ServerModState.MissingLocally => "Lokal fehlend",
        ServerModState.Different => "Abweichend",
        ServerModState.LocalOnly => "Nur lokal",
        _ => "Unbekannt"
    };
}

public enum ServerModState
{
    Current,
    MissingLocally,
    Different,
    LocalOnly
}

namespace EasyCoop.ModManager.Models;

public sealed record ProfileActivationResult(int Activated, int Disabled, IReadOnlyList<string> Missing)
{
    public string Message => Missing.Count == 0
        ? $"Profil aktiviert: {Activated} aktiviert, {Disabled} deaktiviert."
        : $"Profil aktiviert: {Activated} aktiviert, {Disabled} deaktiviert. Fehlend: {Missing.Count}.";
}

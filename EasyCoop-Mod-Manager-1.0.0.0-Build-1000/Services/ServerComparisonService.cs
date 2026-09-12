using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public sealed class ServerComparisonService
{
    public IReadOnlyList<ServerModComparison> Compare(IReadOnlyList<ServerMod> serverMods, IReadOnlyList<ModInfo> localMods)
    {
        var active = localMods.Where(m => m.StorageState == ModStorageState.Active).ToList();
        var results = new List<ServerModComparison>();

        foreach (var serverMod in serverMods)
        {
            var local = active.FirstOrDefault(m => string.Equals(m.FileName, serverMod.FileName, StringComparison.OrdinalIgnoreCase));
            var state = local is null ? ServerModState.MissingLocally
                : Matches(serverMod, local) ? ServerModState.Current
                : ServerModState.Different;
            results.Add(new ServerModComparison { ServerMod = serverMod, LocalMod = local, State = state });
        }

        var serverNames = serverMods.Select(m => m.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        results.AddRange(active.Where(m => !serverNames.Contains(m.FileName)).Select(m => new ServerModComparison
        {
            LocalMod = m,
            State = ServerModState.LocalOnly
        }));

        return results.OrderBy(r => r.State).ThenBy(r => r.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static bool Matches(ServerMod serverMod, ModInfo local)
    {
        if (!string.Equals(local.Status, "OK", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(serverMod.Sha256) && !string.IsNullOrWhiteSpace(local.Sha256))
            return serverMod.Sha256.Equals(local.Sha256, StringComparison.OrdinalIgnoreCase);
        return serverMod.Version.Equals(local.Version, StringComparison.OrdinalIgnoreCase);
    }
}

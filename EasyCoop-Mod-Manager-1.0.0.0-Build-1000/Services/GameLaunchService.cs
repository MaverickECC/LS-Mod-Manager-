using System.Diagnostics;
using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public static class GameLaunchService
{
    public static string? DetectExecutable(string game)
    {
        var year = game switch { "LS19" => "2019", "LS22" => "2022", _ => "2025" };
        var exe = $"FarmingSimulator{year}.exe";
        var folder = $"Farming Simulator {game[2..]}";
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        }.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);
        var candidates = roots.SelectMany(root => new[]
        {
            Path.Combine(root, "Steam", "steamapps", "common", folder, exe),
            Path.Combine(root, folder, exe),
            Path.Combine(root, "GIANTS Software", folder, exe),
            Path.Combine(root, "Epic Games", folder.Replace(" ", ""), exe)
        });
        return candidates.FirstOrDefault(File.Exists);
    }

    public static void Start(GameLaunchSettings settings)
    {
        if (!File.Exists(settings.ExecutablePath)) throw new FileNotFoundException("Die Spiel-EXE wurde nicht gefunden.", settings.ExecutablePath);
        Process.Start(new ProcessStartInfo
        {
            FileName = settings.ExecutablePath,
            Arguments = settings.Arguments,
            WorkingDirectory = Path.GetDirectoryName(settings.ExecutablePath)!,
            UseShellExecute = false
        });
    }
}

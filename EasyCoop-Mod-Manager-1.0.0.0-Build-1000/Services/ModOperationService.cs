using System.IO.Compression;
using EasyCoop.ModManager.Models;

namespace EasyCoop.ModManager.Services;

public sealed class ModOperationService
{
    public async Task InstallAsync(string sourcePath, string modsPath, bool createBackup = true, string? backupRoot = null)
    {
        ValidateZip(sourcePath);
        Directory.CreateDirectory(modsPath);
        var destination = Path.Combine(modsPath, Path.GetFileName(sourcePath));
        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Diese Mod befindet sich bereits im aktiven Modordner.");
        if (File.Exists(destination) && createBackup) Backup(destination, modsPath, backupRoot);
        await CopyAsync(sourcePath, destination);
    }

    public void Disable(ModInfo mod, string modsPath, bool createBackup = true, string? backupRoot = null)
    {
        RequireState(mod, ModStorageState.Active);
        MoveSafely(mod.FullPath, Path.Combine(modsPath, ".easycoop-disabled"), modsPath, createBackup, backupRoot);
    }

    public void Activate(ModInfo mod, string modsPath, bool createBackup = true, string? backupRoot = null)
    {
        if (mod.StorageState == ModStorageState.Active) return;
        var destination = Path.Combine(modsPath, mod.FileName);
        if (File.Exists(destination) && createBackup) Backup(destination, modsPath, backupRoot);
        Directory.CreateDirectory(modsPath);
        File.Move(mod.FullPath, destination, true);
    }

    public void MoveToTrash(ModInfo mod, string modsPath, bool createBackup = true, string? backupRoot = null) =>
        MoveSafely(mod.FullPath, Path.Combine(modsPath, ".easycoop-trash"), modsPath, createBackup, backupRoot);

    public void DeletePermanently(ModInfo mod, string modsPath, bool createBackup = true, string? backupRoot = null)
    {
        if (createBackup) Backup(mod.FullPath, modsPath, backupRoot);
        File.Delete(mod.FullPath);
    }

    private static void MoveSafely(string source, string targetFolder, string modsPath, bool createBackup, string? backupRoot)
    {
        Directory.CreateDirectory(targetFolder);
        if (createBackup) Backup(source, modsPath, backupRoot);
        var destination = Path.Combine(targetFolder, Path.GetFileName(source));
        if (File.Exists(destination))
        {
            if (createBackup) Backup(destination, modsPath, backupRoot);
            File.Delete(destination);
        }
        File.Move(source, destination);
    }

    private static void Backup(string source, string modsPath, string? backupRoot = null)
    {
        if (!File.Exists(source)) return;
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var root = string.IsNullOrWhiteSpace(backupRoot) ? Path.Combine(modsPath, ".easycoop-backups") : backupRoot;
        var folder = Path.Combine(root, stamp);
        Directory.CreateDirectory(folder);
        File.Copy(source, UniquePath(folder, Path.GetFileName(source)), false);
    }

    private static string UniquePath(string folder, string fileName)
    {
        var destination = Path.Combine(folder, fileName);
        if (!File.Exists(destination)) return destination;
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        return Path.Combine(folder, $"{name}-{DateTime.Now:yyyyMMdd-HHmmssfff}{extension}");
    }

    private static async Task CopyAsync(string source, string destination)
    {
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        await using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        await input.CopyToAsync(output);
    }

    private static void ValidateZip(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Nur ZIP-Mods können installiert werden.");
        using var archive = ZipFile.OpenRead(path);
        if (!archive.Entries.Any(e => string.Equals(e.FullName, "modDesc.xml", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Die ausgewählte Datei enthält keine modDesc.xml.");
    }

    private static void RequireState(ModInfo mod, ModStorageState state)
    {
        if (mod.StorageState != state)
            throw new InvalidOperationException("Diese Aktion ist für den aktuellen Modstatus nicht verfügbar.");
    }
}

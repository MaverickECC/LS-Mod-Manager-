namespace EasyCoop.ModManager.Models;

public sealed class SavegameInfo
{
    public required string SlotName { get; init; }
    public required string DisplayName { get; init; }
    public required string MapName { get; init; }
    public required string FolderPath { get; init; }
    public required DateTime LastModified { get; init; }
    public required long SizeBytes { get; init; }
    public decimal? Money { get; init; }
    public string FarmName { get; init; } = "";
    public bool CanEditMoney { get; init; }
    public bool CanEditSavegameName { get; init; }
    public bool CanEditFarmName { get; init; }
    public string LastModifiedText => LastModified.ToString("dd.MM.yyyy HH:mm");
    public string SizeText => SizeBytes < 1_048_576
        ? $"{SizeBytes / 1024d:0.0} KB"
        : $"{SizeBytes / 1_048_576d:0.0} MB";
}

public sealed class SavegameBasicValues
{
    public required string SavegameName { get; init; }
    public required string FarmName { get; init; }
    public required decimal Money { get; init; }
}

public sealed class SavegameBackupInfo
{
    public required string Name { get; init; }
    public required string FolderPath { get; init; }
    public required DateTime Created { get; init; }
    public required long SizeBytes { get; init; }
    public string CreatedText => Created.ToString("dd.MM.yyyy HH:mm:ss");
    public string SizeText => $"{SizeBytes / 1_048_576d:0.0} MB";
}

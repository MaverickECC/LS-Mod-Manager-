namespace EasyCoop.ModManager.Models;

public sealed class AppSettings
{
    public string PortalApiBaseUrl { get; set; } = "https://example.invalid/api/mod-manager/";
    public Dictionary<string, string> CustomModPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<ModFolderDefinition>> ModFolders { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ActiveModFolderIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, GameLaunchSettings> GameLaunchSettings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public UserPreferences Preferences { get; set; } = new();
}

public sealed class UserPreferences
{
    public string Language { get; set; } = "Deutsch";
    public bool StartWithWindows { get; set; }
    public string CloseBehavior { get; set; } = "Beenden";
    public bool ReopenLastModFolder { get; set; } = true;
    public string DefaultGame { get; set; } = "LS25";
    public string Theme { get; set; } = "Dunkel";
    public string ModView { get; set; } = "Kacheln";
    public int ModImageSize { get; set; } = 160;
    public int ModsPerRow { get; set; } = 4;
    public string VisibleModInformation { get; set; } = "Titel, Version, Autor, Status";
    public string UserDirectory { get; set; } = "";
    public string SavegameLogPath { get; set; } = "";
    public string GameProvider { get; set; } = "Automatisch";
    public string FolderSwitchBehavior { get; set; } = "Verknüpfen";
    public string StartPlatform { get; set; } = "Direkt";
    public bool MinimizeOnGameStart { get; set; } = true;
    public bool LockModsWhilePlaying { get; set; } = true;
    public bool AnalyzeLogAfterGame { get; set; } = true;
    public bool AnalyzeOnScan { get; set; } = true;
    public string AnalysisDepth { get; set; } = "Standard";
    public bool DetectDuplicatesAndDamage { get; set; } = true;
    public bool CheckDependenciesAndConflicts { get; set; } = true;
    public string WarningLevel { get; set; } = "Normal";
    public bool AutomaticBackups { get; set; } = true;
    public string BackupPath { get; set; } = "";
    public string BackupInterval { get; set; } = "Vor Änderungen";
    public int BackupRetention { get; set; } = 10;
    public bool BackupMods { get; set; } = true;
    public bool BackupSavegames { get; set; } = true;
    public bool CheckEasyCoopUpdates { get; set; } = true;
    public bool AutomaticUpdates { get; set; }
    public string UpdateChannel { get; set; } = "Stabil";
    public bool CheckModUpdates { get; set; } = true;
    public bool BackupOldModVersion { get; set; } = true;
    public bool NotifyAnalysisComplete { get; set; } = true;
    public bool NotifyBrokenMods { get; set; } = true;
    public bool NotifyModUpdate { get; set; } = true;
    public bool NotifyBackupComplete { get; set; } = true;
    public bool DesktopNotifications { get; set; } = true;
    public bool NotificationSound { get; set; }
    public bool ConfirmBeforeDelete { get; set; } = true;
    public bool UseTrash { get; set; } = true;
    public bool ReportUnknownFiles { get; set; } = true;
    public string QuarantinePath { get; set; } = "";
    public bool BackupOriginalFiles { get; set; } = true;
    public bool DebugLogging { get; set; }
}

public sealed class GameLaunchSettings
{
    public string ExecutablePath { get; set; } = "";
    public string Arguments { get; set; } = "";
}

public sealed class ModFolderDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Modordner";
    public string Path { get; set; } = "";
    public bool IsDefault { get; set; }
    public string DisplayText => $"{Name}  ·  {Path}";
}

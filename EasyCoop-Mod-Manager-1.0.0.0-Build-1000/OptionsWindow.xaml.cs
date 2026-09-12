using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using EasyCoop.ModManager.Models;
using EasyCoop.ModManager.Services;
using Microsoft.Win32;

namespace EasyCoop.ModManager;

public partial class OptionsWindow : Window
{
    public event Action? SettingsSaved;
    private readonly AppSettingsService _service = new();
    private AppSettings _settings;
    private readonly string _game;
    private bool _loading;

    public OptionsWindow(string selectedGame)
    {
        InitializeComponent();
        _game = selectedGame;
        _settings = _service.Load();
        Navigation.SelectedIndex = 0;
        LoadControls();
    }

    private void Navigation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    { if (Pages is not null && Navigation.SelectedIndex >= 0) Pages.SelectedIndex = Navigation.SelectedIndex; }

    private void LoadControls()
    {
        _loading = true;
        var p = _settings.Preferences;
        Select(LanguageBox, p.Language); AutostartBox.IsChecked = p.StartWithWindows; Select(CloseBehaviorBox, p.CloseBehavior);
        ReopenFolderBox.IsChecked = p.ReopenLastModFolder; Select(DefaultGameBox, p.DefaultGame);
        Select(ThemeBox, p.Theme); Select(ViewBox, p.ModView); ImageSizeSlider.Value = p.ModImageSize; ModsPerRowSlider.Value = p.ModsPerRow; VisibleInfoBox.Text = p.VisibleModInformation;
        Ls19PathBox.Text = Launch("LS19").ExecutablePath; Ls22PathBox.Text = Launch("LS22").ExecutablePath; Ls25PathBox.Text = Launch("LS25").ExecutablePath;
        UserDirectoryBox.Text = p.UserDirectory; SavegameLogBox.Text = p.SavegameLogPath; Select(ProviderBox, p.GameProvider);
        RefreshFolders(); Select(FolderSwitchBox, p.FolderSwitchBehavior);
        Select(StartPlatformBox, p.StartPlatform); Select(LaunchGameBox, _game); LaunchArgumentsBox.Text = Launch(_game).Arguments;
        MinimizeOnStartBox.IsChecked = p.MinimizeOnGameStart; LockModsBox.IsChecked = p.LockModsWhilePlaying; AnalyzeLogBox.IsChecked = p.AnalyzeLogAfterGame;
        AnalyzeOnScanBox.IsChecked = p.AnalyzeOnScan; Select(AnalysisDepthBox, p.AnalysisDepth); DetectDamageBox.IsChecked = p.DetectDuplicatesAndDamage; CheckDependenciesBox.IsChecked = p.CheckDependenciesAndConflicts; Select(WarningLevelBox, p.WarningLevel);
        AutomaticBackupsBox.IsChecked = p.AutomaticBackups; BackupPathBox.Text = p.BackupPath; Select(BackupIntervalBox, p.BackupInterval); BackupRetentionBox.Text = p.BackupRetention.ToString(); BackupModsBox.IsChecked = p.BackupMods; BackupSavegamesBox.IsChecked = p.BackupSavegames;
        CheckUpdatesBox.IsChecked = p.CheckEasyCoopUpdates; AutomaticUpdatesBox.IsChecked = p.AutomaticUpdates; Select(UpdateChannelBox, p.UpdateChannel); CheckModUpdatesBox.IsChecked = p.CheckModUpdates; BackupOldVersionBox.IsChecked = p.BackupOldModVersion;
        NotifyAnalysisBox.IsChecked = p.NotifyAnalysisComplete; NotifyBrokenBox.IsChecked = p.NotifyBrokenMods; NotifyUpdateBox.IsChecked = p.NotifyModUpdate; NotifyBackupBox.IsChecked = p.NotifyBackupComplete; DesktopNotificationsBox.IsChecked = p.DesktopNotifications; NotificationSoundBox.IsChecked = p.NotificationSound;
        ConfirmDeleteBox.IsChecked = p.ConfirmBeforeDelete; UseTrashBox.IsChecked = p.UseTrash; ReportUnknownBox.IsChecked = p.ReportUnknownFiles; QuarantinePathBox.Text = p.QuarantinePath; BackupOriginalsBox.IsChecked = p.BackupOriginalFiles; DebugLoggingBox.IsChecked = p.DebugLogging;
        _loading = false; UpdatePreviewValues();
    }

    private void ReadControls()
    {
        var p = _settings.Preferences;
        p.Language = Value(LanguageBox); p.StartWithWindows = On(AutostartBox); p.CloseBehavior = Value(CloseBehaviorBox); p.ReopenLastModFolder = On(ReopenFolderBox); p.DefaultGame = Value(DefaultGameBox);
        p.Theme = Value(ThemeBox); p.ModView = Value(ViewBox); p.ModImageSize = (int)ImageSizeSlider.Value; p.ModsPerRow = (int)ModsPerRowSlider.Value; p.VisibleModInformation = VisibleInfoBox.Text.Trim();
        Launch("LS19").ExecutablePath = Ls19PathBox.Text.Trim(); Launch("LS22").ExecutablePath = Ls22PathBox.Text.Trim(); Launch("LS25").ExecutablePath = Ls25PathBox.Text.Trim();
        p.UserDirectory = UserDirectoryBox.Text.Trim(); p.SavegameLogPath = SavegameLogBox.Text.Trim(); p.GameProvider = Value(ProviderBox); p.FolderSwitchBehavior = Value(FolderSwitchBox);
        p.StartPlatform = Value(StartPlatformBox); Launch(Value(LaunchGameBox)).Arguments = LaunchArgumentsBox.Text.Trim(); p.MinimizeOnGameStart = On(MinimizeOnStartBox); p.LockModsWhilePlaying = On(LockModsBox); p.AnalyzeLogAfterGame = On(AnalyzeLogBox);
        p.AnalyzeOnScan = On(AnalyzeOnScanBox); p.AnalysisDepth = Value(AnalysisDepthBox); p.DetectDuplicatesAndDamage = On(DetectDamageBox); p.CheckDependenciesAndConflicts = On(CheckDependenciesBox); p.WarningLevel = Value(WarningLevelBox);
        p.AutomaticBackups = On(AutomaticBackupsBox); p.BackupPath = BackupPathBox.Text.Trim(); p.BackupInterval = Value(BackupIntervalBox); p.BackupRetention = int.TryParse(BackupRetentionBox.Text, out var retention) ? Math.Clamp(retention, 1, 999) : 10; p.BackupMods = On(BackupModsBox); p.BackupSavegames = On(BackupSavegamesBox);
        p.CheckEasyCoopUpdates = On(CheckUpdatesBox); p.AutomaticUpdates = On(AutomaticUpdatesBox); p.UpdateChannel = Value(UpdateChannelBox); p.CheckModUpdates = On(CheckModUpdatesBox); p.BackupOldModVersion = On(BackupOldVersionBox);
        p.NotifyAnalysisComplete = On(NotifyAnalysisBox); p.NotifyBrokenMods = On(NotifyBrokenBox); p.NotifyModUpdate = On(NotifyUpdateBox); p.NotifyBackupComplete = On(NotifyBackupBox); p.DesktopNotifications = On(DesktopNotificationsBox); p.NotificationSound = On(NotificationSoundBox);
        p.ConfirmBeforeDelete = On(ConfirmDeleteBox); p.UseTrash = On(UseTrashBox); p.ReportUnknownFiles = On(ReportUnknownBox); p.QuarantinePath = QuarantinePathBox.Text.Trim(); p.BackupOriginalFiles = On(BackupOriginalsBox); p.DebugLogging = On(DebugLoggingBox);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ReadControls(); _service.Save(_settings); ApplyAutostart(_settings.Preferences.StartWithWindows);
        SaveStatus.Text = "Änderungen gespeichert";
        SettingsSaved?.Invoke();
    }
    private void Discard_Click(object sender, RoutedEventArgs e) { _settings = _service.Load(); LoadControls(); SaveStatus.Text = "Änderungen verworfen"; }
    private void Defaults_Click(object sender, RoutedEventArgs e) { _settings.Preferences = new UserPreferences(); LoadControls(); SaveStatus.Text = "Standardwerte geladen – noch nicht gespeichert"; }

    private void DetectGamePaths_Click(object sender, RoutedEventArgs e)
    {
        Ls19PathBox.Text = GameLaunchService.DetectExecutable("LS19") ?? Ls19PathBox.Text;
        Ls22PathBox.Text = GameLaunchService.DetectExecutable("LS22") ?? Ls22PathBox.Text;
        Ls25PathBox.Text = GameLaunchService.DetectExecutable("LS25") ?? Ls25PathBox.Text;
        SaveStatus.Text = "Spielepfade wurden geprüft";
    }
    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFolderDialog { Title = $"Modordner für {_game} hinzufügen" };
        if (picker.ShowDialog(this) != true) return;
        var dialog = new ModFolderNameDialog(Path.GetFileName(picker.FolderName)) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var folders = Folders(); folders.Add(new ModFolderDefinition { Name = dialog.FolderName, Path = picker.FolderName }); RefreshFolders();
    }
    private void EditFolder_Click(object sender, RoutedEventArgs e)
    {
        if (FoldersList.SelectedItem is not ModFolderDefinition folder || folder.IsDefault) return;
        var picker = new OpenFolderDialog { Title = "Modordner bearbeiten", InitialDirectory = Directory.Exists(folder.Path) ? folder.Path : null };
        if (picker.ShowDialog(this) != true) return;
        var dialog = new ModFolderNameDialog(folder.Name) { Owner = this }; if (dialog.ShowDialog() != true) return;
        folder.Name = dialog.FolderName; folder.Path = picker.FolderName; RefreshFolders(folder.Id);
    }
    private void SetDefaultFolder_Click(object sender, RoutedEventArgs e)
    {
        if (FoldersList.SelectedItem is not ModFolderDefinition folder) return;
        _settings.ActiveModFolderIds[_game] = folder.Id; SaveStatus.Text = $"„{folder.Name}“ wird der Standardordner";
    }
    private void DetectFolders_Click(object sender, RoutedEventArgs e) { RefreshFolders(); SaveStatus.Text = "Standard-Modordner wurde automatisch erkannt"; }

    private void LaunchGameBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || LaunchArgumentsBox is null) return;
        LaunchArgumentsBox.Text = Launch(Value(LaunchGameBox)).Arguments;
    }
    private void LaunchArgumentsBox_TextChanged(object sender, TextChangedEventArgs e)
    { if (!_loading && LaunchGameBox?.SelectedItem is not null) Launch(Value(LaunchGameBox)).Arguments = LaunchArgumentsBox.Text; }
    private void PreviewValue_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        => UpdatePreviewValues();
    private void UpdatePreviewValues()
    { if (ImageSizeText is not null) ImageSizeText.Text = $"{(int)ImageSizeSlider.Value} Pixel"; if (ModsPerRowText is not null) ModsPerRowText.Text = $"{(int)ModsPerRowSlider.Value} Mods"; }

    private void RestoreBackups_Click(object sender, RoutedEventArgs e) => OpenFolder(BackupPathBox.Text, "Es ist noch kein Backup-Speicherort festgelegt.");
    private void CheckUpdates_Click(object sender, RoutedEventArgs e) => MessageBox.Show(this, "Die Updateprüfung wird über den konfigurierten EasyCoop-Updatekanal ausgeführt, sobald eine Updateadresse hinterlegt ist.", "EasyCoop Mod Manager");
    private void OpenQuarantine_Click(object sender, RoutedEventArgs e) => OpenFolder(QuarantinePathBox.Text, "Es ist noch kein Quarantäneordner festgelegt.");
    private void OpenLogs_Click(object sender, RoutedEventArgs e) => OpenFolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyCoop", "Mod Manager", "logs"), "");
    private void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyCoop", "Mod Manager", "cache");
        if (Directory.Exists(path)) Directory.Delete(path, true); SaveStatus.Text = "Cache geleert";
    }
    private void RebuildDatabase_Click(object sender, RoutedEventArgs e) { SaveStatus.Text = "Moddatenbank wird beim nächsten Einlesen neu aufgebaut"; DialogResult = true; }
    private void ExportSettings_Click(object sender, RoutedEventArgs e)
    {
        ReadControls(); var dialog = new SaveFileDialog { Filter = "JSON-Datei (*.json)|*.json", FileName = "EasyCoop-Einstellungen.json" };
        if (dialog.ShowDialog(this) == true) File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true }));
    }
    private void ImportSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "JSON-Datei (*.json)|*.json" }; if (dialog.ShowDialog(this) != true) return;
        try { _settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(dialog.FileName)) ?? new AppSettings(); _settings.Preferences ??= new UserPreferences(); LoadControls(); SaveStatus.Text = "Einstellungen importiert – noch nicht gespeichert"; }
        catch (JsonException) { MessageBox.Show(this, "Die Einstellungsdatei ist ungültig.", "EasyCoop Mod Manager", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private void ResetEasyCoop_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Alle EasyCoop-Einstellungen zurücksetzen? Mods und Spielstände bleiben erhalten.", "EasyCoop zurücksetzen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        _settings = new AppSettings(); _service.Save(_settings); ApplyAutostart(false); LoadControls(); SaveStatus.Text = "EasyCoop-Einstellungen zurückgesetzt";
    }

    private GameLaunchSettings Launch(string game)
    {
        if (!_settings.GameLaunchSettings.TryGetValue(game, out var launch)) _settings.GameLaunchSettings[game] = launch = new GameLaunchSettings();
        return launch;
    }
    private List<ModFolderDefinition> Folders() => ModFolderService.GetFolders(_settings, _game, DefaultModsPath(_game));
    private void RefreshFolders(string? id = null) { var folders = Folders().ToList(); FoldersList.ItemsSource = folders; FoldersList.SelectedItem = folders.FirstOrDefault(f => f.Id == id) ?? folders.FirstOrDefault(f => f.Id == _settings.ActiveModFolderIds.GetValueOrDefault(_game)) ?? folders.FirstOrDefault(); }
    private static string DefaultModsPath(string game) { var year = game switch { "LS19" => "2019", "LS22" => "2022", _ => "2025" }; return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", $"FarmingSimulator{year}", "mods"); }
    private static bool On(CheckBox box) => box.IsChecked == true;
    private static string Value(ComboBox box) => (box.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
    private static void Select(ComboBox box, string value) { box.SelectedItem = box.Items.Cast<ComboBoxItem>().FirstOrDefault(item => string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase)) ?? box.Items.Cast<ComboBoxItem>().FirstOrDefault(); }
    private static void OpenFolder(string path, string missing)
    {
        if (string.IsNullOrWhiteSpace(path)) { MessageBox.Show(missing); return; }
        Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
    private static void ApplyAutostart(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (enabled && !string.IsNullOrWhiteSpace(Environment.ProcessPath)) key?.SetValue("EasyCoopModManager", $"\"{Environment.ProcessPath}\""); else key?.DeleteValue("EasyCoopModManager", false);
    }
}

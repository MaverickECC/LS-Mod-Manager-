using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using System.Diagnostics;
using EasyCoop.ModManager.Models;
using EasyCoop.ModManager.Services;

namespace EasyCoop.ModManager;

public partial class MainWindow : Window
{
    private readonly ModScannerService _scanner = new();
    private readonly ModOperationService _operations = new();
    private readonly ProfileService _profileService = new();
    private readonly List<ModProfile> _profiles;
    private readonly ProfileActivationService _profileActivation;
    private readonly ModAnalysisService _analysis = new();
    private readonly AppSettingsService _settingsService = new();
    private readonly DiagnosticsService _diagnostics = new();
    private readonly ObservableCollection<ModInfo> _mods = new();
    private readonly ICollectionView _modsView;
    private GameInstallation? _selectedGame;
    private bool _updatingModFolders;
    private bool _allowClose;
    public double TileWidth { get; private set; } = 205;
    public double TileHeight { get; private set; } = 238;
    public double ModImageSize { get; private set; } = 118;
    public bool ShowAuthor { get; private set; } = true;
    public bool ShowVersion { get; private set; } = true;
    public bool ShowStatus { get; private set; } = true;
    public bool ShowAnalysis { get; private set; } = true;

    public MainWindow()
    {
        InitializeComponent();
        _profiles = _profileService.Load().ToList();
        _profileActivation = new ProfileActivationService(_operations);
        _modsView = CollectionViewSource.GetDefaultView(_mods);
        _modsView.Filter = FilterMod;
        ModsGrid.ItemsSource = _modsView;
        var settings = _settingsService.Load();
        ApplySettings(settings.Preferences);
        GamesList.ItemsSource = GameDetectionService.Detect(settings);
        GamesList.SelectedItem = GamesList.Items.Cast<GameInstallation>().FirstOrDefault(g => g.ShortName == settings.Preferences.DefaultGame);
        if (GamesList.SelectedItem is null) GamesList.SelectedIndex = 2;
        Closing += MainWindow_Closing;
        SizeChanged += (_, _) => ApplyModView(_settingsService.Load().Preferences);
    }

    private async void Options_Click(object sender, RoutedEventArgs e)
    {
        var window = new OptionsWindow(_selectedGame?.ShortName ?? "LS25") { Owner = this };
        window.SettingsSaved += () =>
        {
            var current = _settingsService.Load();
            ApplySettings(current.Preferences);
            _ = LoadModsAsync();
        };
        window.ShowDialog();
        var settings = _settingsService.Load();
        ApplySettings(settings.Preferences);
        var selectedGame = _selectedGame?.ShortName ?? settings.Preferences.DefaultGame;
        var games = GameDetectionService.Detect(settings);
        GamesList.ItemsSource = games;
        GamesList.SelectedItem = games.FirstOrDefault(g => g.ShortName == selectedGame) ?? games.FirstOrDefault();
        await LoadModsAsync();
    }

    private void About_Click(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();

    private void ApplySettings(UserPreferences preferences)
    {
        ApplyTheme(preferences.Theme);
        ApplyModView(preferences);
    }

    private void ApplyTheme(string theme)
    {
        var light = theme == "Hell" || (theme == "System" && IsSystemLightTheme());
        SetBrush("EasyCoopDark", light ? "#EEF3EF" : "#111815");
        SetBrush("EasyCoopPanel", light ? "#DCE7DF" : "#1C2822");
        SetBrush("EasyCoopText", light ? "#17201B" : "#FFFFFF");
        Background = (System.Windows.Media.Brush)Application.Current.Resources["EasyCoopDark"];
        Foreground = light ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;
        ModsGrid.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(light ? "#F5F8F6" : "#17201B"));
    }

    private static void SetBrush(string key, string color)
    {
        Application.Current.Resources[key] = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
    }

    private static bool IsSystemLightTheme()
    {
        var value = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0);
        return value is int number && number != 0;
    }

    private void ApplyModView(UserPreferences preferences)
    {
        ModImageSize = Math.Clamp(preferences.ModImageSize, 80, 260);
        var available = ActualWidth > 500 ? ActualWidth - 360 : 700;
        TileWidth = Math.Max(ModImageSize + 40, available / Math.Clamp(preferences.ModsPerRow, 2, 8) - 18);
        TileHeight = ModImageSize + 125;
        var visible = preferences.VisibleModInformation ?? "";
        ShowAuthor = visible.Contains("Autor", StringComparison.OrdinalIgnoreCase);
        ShowVersion = visible.Contains("Version", StringComparison.OrdinalIgnoreCase);
        ShowStatus = visible.Contains("Status", StringComparison.OrdinalIgnoreCase) || visible.Contains("Zustand", StringComparison.OrdinalIgnoreCase);
        ShowAnalysis = visible.Contains("Analyse", StringComparison.OrdinalIgnoreCase) || visible.Contains("Prüfung", StringComparison.OrdinalIgnoreCase);
        ModsGrid.ItemsPanel = (ItemsPanelTemplate)FindResource(preferences.ModView == "Liste" ? "ListPanel" : "TilePanel");
        ModsGrid.ItemTemplate = (DataTemplate)FindResource(preferences.ModView == "Liste" ? "ListModTemplate" : "TileModTemplate");
        ModsGrid.Items.Refresh();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        var behavior = _settingsService.Load().Preferences.CloseBehavior;
        if (behavior == "Minimieren") { e.Cancel = true; WindowState = WindowState.Minimized; return; }
        if (behavior == "Nachfragen" && MessageBox.Show(this, "EasyCoop wirklich beenden?", "EasyCoop Mod Manager", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) e.Cancel = true;
        else _allowClose = true;
    }

    private async void GamesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedGame = GamesList.SelectedItem as GameInstallation;
        if (_selectedGame is null) return;
        PageTitle.Text = $"{_selectedGame.ShortName} Mods";
        RefreshModFolders();
        RefreshProfiles();
        await LoadModsAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadModsAsync();

    private async void OpenServers_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null) return;
        var window = new DedicatedServersWindow(_selectedGame, _operations) { Owner = this };
        window.ShowDialog();
        await LoadModsAsync();
    }

    private void OpenSavegames_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null) return;
        new SavegamesWindow(_selectedGame) { Owner = this }.ShowDialog();
    }

    private void ConfigureGame_Click(object sender, RoutedEventArgs e) => ShowGameLaunchDialog(false);

    private void StartGame_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null) return;
        var settings = _settingsService.Load();
        var launch = GetLaunchSettings(settings, _selectedGame.ShortName);
        if (!File.Exists(launch.ExecutablePath))
        {
            ShowGameLaunchDialog(true);
            return;
        }
        try
        {
            GameLaunchService.Start(launch);
            StatusText.Text = $"{_selectedGame.ShortName} wurde gestartet.";
            if (settings.Preferences.MinimizeOnGameStart) WindowState = WindowState.Minimized;
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ShowGameLaunchDialog(bool startAfterConfiguration)
    {
        if (_selectedGame is null) return;
        var settings = _settingsService.Load();
        var launch = GetLaunchSettings(settings, _selectedGame.ShortName);
        var dialog = new GameLaunchDialog(_selectedGame.ShortName, launch) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        settings.GameLaunchSettings[_selectedGame.ShortName] = dialog.Settings;
        _settingsService.Save(settings);
        if (!dialog.StartRequested && !startAfterConfiguration)
        {
            StatusText.Text = $"Startkonfiguration für {_selectedGame.ShortName} gespeichert.";
            return;
        }
        try
        {
            GameLaunchService.Start(dialog.Settings);
            StatusText.Text = $"{_selectedGame.ShortName} wurde gestartet.";
            if (settings.Preferences.MinimizeOnGameStart) WindowState = WindowState.Minimized;
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private static GameLaunchSettings GetLaunchSettings(AppSettings settings, string game)
    {
        if (!settings.GameLaunchSettings.TryGetValue(game, out var launch))
        {
            launch = new GameLaunchSettings { ExecutablePath = GameLaunchService.DetectExecutable(game) ?? "" };
            settings.GameLaunchSettings[game] = launch;
        }
        return launch;
    }

    private async void ChooseModsPath_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null) return;
        if (!EnsureModsUnlocked()) return;
        var dialog = new OpenFolderDialog
        {
            Title = $"Modordner für {_selectedGame.ShortName} auswählen",
            Multiselect = false
        };
        if (Directory.Exists(_selectedGame.ModsPath)) dialog.InitialDirectory = _selectedGame.ModsPath;
        if (dialog.ShowDialog(this) != true) return;
        var nameDialog = new ModFolderNameDialog(Path.GetFileName(dialog.FolderName)) { Owner = this };
        if (nameDialog.ShowDialog() != true) return;

        var selectedGame = _selectedGame.ShortName;
        var settings = _settingsService.Load();
        var folders = ModFolderService.GetFolders(settings, selectedGame, GetDefaultModsPath(selectedGame));
        var existing = folders.FirstOrDefault(f => string.Equals(Path.GetFullPath(f.Path), Path.GetFullPath(dialog.FolderName), StringComparison.OrdinalIgnoreCase));
        var folder = existing ?? new ModFolderDefinition { Name = nameDialog.FolderName, Path = Path.GetFullPath(dialog.FolderName) };
        if (existing is null) folders.Add(folder); else existing.Name = nameDialog.FolderName;
        settings.ActiveModFolderIds[selectedGame] = folder.Id;
        settings.CustomModPaths.Remove(selectedGame);
        _settingsService.Save(settings);
        ApplySelectedFolder(folder);
        RefreshModFolders(folder.Id);
        await LoadModsAsync();
    }

    private async void ModFoldersBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingModFolders || _selectedGame is null || ModFoldersBox.SelectedItem is not ModFolderDefinition folder) return;
        if (!EnsureModsUnlocked()) { RefreshModFolders(); return; }
        var settings = _settingsService.Load();
        settings.ActiveModFolderIds[_selectedGame.ShortName] = folder.Id;
        _settingsService.Save(settings);
        ApplySelectedFolder(folder);
        await LoadModsAsync();
    }

    private async void RemoveModsPath_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null || ModFoldersBox.SelectedItem is not ModFolderDefinition folder) return;
        if (!EnsureModsUnlocked()) return;
        if (folder.IsDefault) { ShowInfo("Der Standard-Modordner kann nicht aus der Liste entfernt werden."); return; }
        if (!Confirm($"Soll „{folder.Name}“ aus EasyCoop entfernt werden? Der echte Ordner und seine Mods bleiben erhalten.")) return;
        var settings = _settingsService.Load();
        var folders = ModFolderService.GetFolders(settings, _selectedGame.ShortName, GetDefaultModsPath(_selectedGame.ShortName));
        folders.RemoveAll(f => f.Id == folder.Id);
        var next = folders[0];
        settings.ActiveModFolderIds[_selectedGame.ShortName] = next.Id;
        _settingsService.Save(settings);
        ApplySelectedFolder(next);
        RefreshModFolders(next.Id);
        await LoadModsAsync();
    }

    private void RefreshModFolders(string? selectedId = null)
    {
        if (_selectedGame is null) return;
        var settings = _settingsService.Load();
        var folders = ModFolderService.GetFolders(settings, _selectedGame.ShortName, GetDefaultModsPath(_selectedGame.ShortName));
        selectedId ??= settings.ActiveModFolderIds.GetValueOrDefault(_selectedGame.ShortName);
        var selected = folders.FirstOrDefault(f => f.Id == selectedId) ?? folders[0];
        settings.ActiveModFolderIds[_selectedGame.ShortName] = selected.Id;
        _settingsService.Save(settings);
        _updatingModFolders = true;
        ModFoldersBox.ItemsSource = folders.ToList();
        ModFoldersBox.SelectedItem = ModFoldersBox.Items.Cast<ModFolderDefinition>().FirstOrDefault(f => f.Id == selected.Id);
        _updatingModFolders = false;
        ApplySelectedFolder(selected);
    }

    private void ApplySelectedFolder(ModFolderDefinition folder)
    {
        if (_selectedGame is null) return;
        _selectedGame.ModsPath = Path.GetFullPath(folder.Path);
        _selectedGame.IsCustomPath = !folder.IsDefault;
        GamesList.Items.Refresh();
    }

    private static string GetDefaultModsPath(string game)
    {
        var year = game switch { "LS19" => "2019", "LS22" => "2022", _ => "2025" };
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", $"FarmingSimulator{year}", "mods");
    }

    private void OpenBackups_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null) return;
        var folder = Path.Combine(_selectedGame.ModsPath, ".easycoop-backups");
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

    private void ExportDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "EasyCoop-Diagnosepaket speichern",
            Filter = "ZIP-Datei (*.zip)|*.zip",
            FileName = $"EasyCoop-Mod-Manager-Diagnose-{DateTime.Now:yyyyMMdd-HHmm}.zip"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            _diagnostics.Export(dialog.FileName, GameDetectionService.Detect(_settingsService.Load()), _mods.ToList());
            ShowInfo("Das Diagnosepaket wurde erstellt. Es enthält keine Passwörter oder Zugangstoken.");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null) return;
        if (!EnsureModsUnlocked()) return;
        var dialog = new OpenFileDialog { Filter = "LS Mod (*.zip)|*.zip", Multiselect = true, Title = "Mods installieren" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            StatusText.Text = "Installiere ausgewählte Mods …";
            var preferences = _settingsService.Load().Preferences;
            foreach (var file in dialog.FileNames) await _operations.InstallAsync(file, _selectedGame.ModsPath, ShouldBackup(preferences), preferences.BackupPath);
            await LoadModsAsync();
            MessageBox.Show(this, $"{dialog.FileNames.Length} Mod(s) wurden installiert.", "EasyCoop Mod Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void Disable_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelection(out var mod) || _selectedGame is null) return;
        if (!EnsureModsUnlocked()) return;
        if (mod.StorageState != ModStorageState.Active) { ShowInfo("Nur aktive Mods können deaktiviert werden."); return; }
        var preferences = _settingsService.Load().Preferences;
        if (preferences.ConfirmBeforeDelete && !Confirm($"Soll „{mod.Title}“ deaktiviert werden?")) return;
        try { _operations.Disable(mod, _selectedGame.ModsPath, ShouldBackup(preferences), preferences.BackupPath); await LoadModsAsync(); }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void Activate_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelection(out var mod) || _selectedGame is null) return;
        if (!EnsureModsUnlocked()) return;
        if (mod.StorageState == ModStorageState.Active) { ShowInfo("Die ausgewählte Mod ist bereits aktiv."); return; }
        var preferences = _settingsService.Load().Preferences;
        try { _operations.Activate(mod, _selectedGame.ModsPath, ShouldBackup(preferences), preferences.BackupPath); await LoadModsAsync(); }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void Trash_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelection(out var mod) || _selectedGame is null) return;
        if (!EnsureModsUnlocked()) return;
        if (mod.StorageState == ModStorageState.Trash) { ShowInfo("Die ausgewählte Mod liegt bereits im Papierkorb."); return; }
        var preferences = _settingsService.Load().Preferences;
        var action = preferences.UseTrash ? "in den EasyCoop-Papierkorb verschoben" : "endgültig gelöscht";
        if (preferences.ConfirmBeforeDelete && !Confirm($"Soll „{mod.Title}“ {action} werden?")) return;
        try
        {
            if (preferences.UseTrash) _operations.MoveToTrash(mod, _selectedGame.ModsPath, ShouldBackup(preferences), preferences.BackupPath);
            else _operations.DeletePermanently(mod, _selectedGame.ModsPath, ShouldBackup(preferences), preferences.BackupPath);
            await LoadModsAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private static bool ShouldBackup(UserPreferences preferences) => preferences.AutomaticBackups && preferences.BackupOriginalFiles && preferences.BackupMods;

    private bool EnsureModsUnlocked()
    {
        if (_selectedGame is null) return false;
        var preferences = _settingsService.Load().Preferences;
        if (!preferences.LockModsWhilePlaying) return true;
        var processName = _selectedGame.ShortName switch { "LS19" => "FarmingSimulator2019", "LS22" => "FarmingSimulator2022", _ => "FarmingSimulator2025" };
        if (Process.GetProcessesByName(processName).Length == 0) return true;
        ShowInfo("Die Mods sind gesperrt, solange das Spiel läuft. Diese Einstellung kann unter Optionen → Spielstart geändert werden.");
        return false;
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null) return;
        var dialog = new ProfileNameDialog { Owner = this };
        if (dialog.ShowDialog() != true) return;
        if (_profiles.Any(p => p.GameShortName == _selectedGame.ShortName &&
                               string.Equals(p.Name, dialog.ProfileName, StringComparison.CurrentCultureIgnoreCase)))
        {
            ShowInfo("Für diese Spielversion existiert bereits ein Profil mit diesem Namen.");
            return;
        }

        var profile = new ModProfile
        {
            Name = dialog.ProfileName,
            GameShortName = _selectedGame.ShortName,
            ModFileNames = ActiveModFileNames()
        };
        _profiles.Add(profile);
        SaveProfiles();
        RefreshProfiles(profile.Id);
        ShowInfo($"Profil „{profile.Name}“ wurde mit {profile.ModFileNames.Count} aktiven Mods erstellt.");
    }

    private void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesBox.SelectedItem is not ModProfile profile) { ShowInfo("Bitte zuerst ein Profil auswählen."); return; }
        profile.ModFileNames = ActiveModFileNames();
        profile.UpdatedUtc = DateTime.UtcNow;
        SaveProfiles();
        RefreshProfiles(profile.Id);
        StatusText.Text = $"Profil „{profile.Name}“ wurde mit {profile.ModFileNames.Count} Mods gespeichert.";
    }

    private async void ActivateProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesBox.SelectedItem is not ModProfile profile || _selectedGame is null)
        { ShowInfo("Bitte zuerst ein Profil auswählen."); return; }
        if (!EnsureModsUnlocked()) return;
        if (!Confirm($"Profil „{profile.Name}“ aktivieren? Die aktuelle Modauswahl wird dabei umgeschaltet.")) return;
        try
        {
            var result = _profileActivation.Activate(profile, _mods, _selectedGame.ModsPath);
            await LoadModsAsync();
            StatusText.Text = result.Message;
            var detail = result.Message;
            if (result.Missing.Count > 0)
                detail += "\n\nNicht verfügbare Mods:\n" + string.Join("\n", result.Missing.Take(20));
            ShowInfo(detail);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesBox.SelectedItem is not ModProfile profile) { ShowInfo("Bitte zuerst ein Profil auswählen."); return; }
        if (!Confirm($"Soll das Profil „{profile.Name}“ gelöscht werden? Die Moddateien bleiben erhalten.")) return;
        _profiles.Remove(profile);
        SaveProfiles();
        RefreshProfiles();
    }

    private void ProfilesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ProfileSummary.Text = ProfilesBox.SelectedItem is ModProfile profile
            ? $"{profile.ModFileNames.Count} Mods · geändert {profile.UpdatedUtc.ToLocalTime():dd.MM.yyyy HH:mm}"
            : "Kein Profil ausgewählt";
    }

    private List<string> ActiveModFileNames() => _mods
        .Where(m => m.StorageState == ModStorageState.Active)
        .Select(m => m.FileName)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private void RefreshProfiles(Guid? selectedId = null)
    {
        if (_selectedGame is null) return;
        selectedId ??= (ProfilesBox.SelectedItem as ModProfile)?.Id;
        var matching = _profiles
            .Where(p => p.GameShortName == _selectedGame.ShortName)
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        ProfilesBox.ItemsSource = matching;
        ProfilesBox.SelectedItem = matching.FirstOrDefault(p => p.Id == selectedId);
        if (ProfilesBox.SelectedItem is null && matching.Count > 0) ProfilesBox.SelectedIndex = 0;
    }

    private void SaveProfiles() => _profileService.Save(_profiles);

    private bool TryGetSelection(out ModInfo mod)
    {
        mod = ModsGrid.SelectedItem as ModInfo ?? null!;
        if (mod is not null) return true;
        ShowInfo("Bitte zuerst eine Mod auswählen.");
        return false;
    }

    private bool Confirm(string message) => MessageBox.Show(this, message, "EasyCoop Mod Manager",
        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private void ShowInfo(string message) => MessageBox.Show(this, message, "EasyCoop Mod Manager",
        MessageBoxButton.OK, MessageBoxImage.Information);

    private void ShowError(Exception ex)
    {
        AppLogger.Error(ex, "Aktion im Hauptfenster");
        StatusText.Text = ex.Message;
        MessageBox.Show(this, ex.Message, "Aktion fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async Task LoadModsAsync()
    {
        if (_selectedGame is null) return;
        StatusText.Text = $"Lese {_selectedGame.ShortName}-Mods ein …";
        _mods.Clear();
        UpdateStatistics(Array.Empty<ModInfo>());

        try
        {
            var mods = await _scanner.ScanAsync(_selectedGame.ModsPath);
            var preferences = _settingsService.Load().Preferences;
            if (preferences.AnalyzeOnScan) _analysis.Analyze(mods, _selectedGame.ShortName, preferences);
            foreach (var mod in mods) _mods.Add(mod);
            ModCount.Text = $"{mods.Count} Mods";
            UpdateStatistics(mods);
            StatusText.Text = _selectedGame.IsAvailable
                ? $"Modordner: {_selectedGame.ModsPath}"
                : $"Modordner wurde nicht gefunden: {_selectedGame.ModsPath}";
            if (preferences.NotifyBrokenMods && mods.Any(m => m.AnalysisStatus == "Fehler"))
                StatusText.Text += $" · {mods.Count(m => m.AnalysisStatus == "Fehler")} fehlerhafte Mod(s) gefunden";
        }
        catch (UnauthorizedAccessException)
        {
            StatusText.Text = "Kein Zugriff auf den Modordner.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Einlesen: {ex.Message}";
        }
    }

    private void UpdateStatistics(IReadOnlyList<ModInfo> mods)
    {
        TotalModsCount.Text = mods.Count.ToString();
        ActiveModsCount.Text = mods.Count(m => m.StorageState == ModStorageState.Active).ToString();
        InactiveModsCount.Text = mods.Count(m => m.StorageState != ModStorageState.Active).ToString();
        WarningsCount.Text = mods.Count(m => m.AnalysisStatus == "Warnung").ToString();
        ErrorsCount.Text = mods.Count(m => m.AnalysisStatus == "Fehler").ToString();
        UnknownModsCount.Text = mods.Count(m => !m.IsRecognized).ToString();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => _modsView.Refresh();

    private void ModsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AnalysisDetails.Text = ModsGrid.SelectedItem is ModInfo mod
            ? mod.IssuesText
            : "Mod auswählen, um den Prüfbericht anzuzeigen.";
    }

    private bool FilterMod(object item)
    {
        if (item is not ModInfo mod) return false;
        var query = SearchBox.Text.Trim();
        return query.Length == 0
            || mod.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || mod.Author.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || mod.FileName.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }
}

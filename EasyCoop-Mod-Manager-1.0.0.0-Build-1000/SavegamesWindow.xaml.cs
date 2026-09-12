using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using EasyCoop.ModManager.Models;
using EasyCoop.ModManager.Services;

namespace EasyCoop.ModManager;

public partial class SavegamesWindow : Window
{
    private readonly GameInstallation _game;
    private readonly SavegameService _service = new();
    private readonly ObservableCollection<SavegameInfo> _savegames = new();
    private readonly ObservableCollection<SavegameBackupInfo> _backups = new();

    public SavegamesWindow(GameInstallation game)
    {
        InitializeComponent();
        _game = game;
        DataContext = game;
        GameText.Text = $"{game.Name} · automatische Erkennung, Sicherung und Wiederherstellung";
        SavegamesGrid.ItemsSource = _savegames;
        BackupsGrid.ItemsSource = _backups;
        RefreshData();
    }

    private void RefreshData()
    {
        try
        {
            _savegames.Clear();
            foreach (var item in _service.Scan(_game.ShortName)) _savegames.Add(item);
            StatusText.Text = _savegames.Count == 0 ? "Keine Spielstände gefunden." : $"{_savegames.Count} Spielstände gefunden.";
            if (_savegames.Count > 0) SavegamesGrid.SelectedIndex = 0;
            else _backups.Clear();
        }
        catch (Exception exception) { ShowError(exception, "Spielstände einlesen"); }
    }

    private void RefreshBackups()
    {
        _backups.Clear();
        if (SavegamesGrid.SelectedItem is not SavegameInfo savegame) return;
        foreach (var item in _service.GetBackups(_game.ShortName, savegame.SlotName)) _backups.Add(item);
    }

    private void SavegamesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshBackups();
        if (SavegamesGrid.SelectedItem is not SavegameInfo savegame)
        {
            SavegameNameBox.Text = FarmNameBox.Text = MoneyBox.Text = "";
            return;
        }
        SavegameNameBox.Text = savegame.DisplayName;
        FarmNameBox.Text = savegame.FarmName;
        MoneyBox.Text = savegame.Money?.ToString("0.##", CultureInfo.CurrentCulture) ?? "";
        SavegameNameBox.IsEnabled = savegame.CanEditSavegameName;
        FarmNameBox.IsEnabled = savegame.CanEditFarmName;
        MoneyBox.IsEnabled = savegame.CanEditMoney;
    }
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshData();

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = SavegamesGrid.SelectedItem is SavegameInfo savegame ? savegame.FolderPath : _service.GetGameUserFolder(_game.ShortName);
        if (!Directory.Exists(folder)) { MessageBox.Show(this, "Der Ordner wurde noch nicht angelegt.", "EasyCoop", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        if (SavegamesGrid.SelectedItem is not SavegameInfo savegame) { ShowSelectionHint(); return; }
        try
        {
            _service.CreateBackup(_game.ShortName, savegame);
            RefreshBackups();
            StatusText.Text = $"Backup von {savegame.SlotName} wurde erstellt.";
        }
        catch (Exception exception) { ShowError(exception, "Backup erstellen"); }
    }

    private void AdvancedEditor_Click(object sender, RoutedEventArgs e)
    {
        if (SavegamesGrid.SelectedItem is not SavegameInfo savegame) { ShowSelectionHint(); return; }
        new AdvancedSavegameWindow(_game, savegame, _service) { Owner = this }.ShowDialog();
        RefreshData();
    }

    private void SaveValues_Click(object sender, RoutedEventArgs e)
    {
        if (SavegamesGrid.SelectedItem is not SavegameInfo savegame) { ShowSelectionHint(); return; }
        if (!savegame.CanEditMoney || !savegame.CanEditSavegameName)
        {
            MessageBox.Show(this, "Die benötigte XML-Struktur dieses Spielstands wurde nicht vollständig erkannt. Es werden keine Änderungen vorgenommen.", "EasyCoop", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!decimal.TryParse(MoneyBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var money) &&
            !decimal.TryParse(MoneyBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out money))
        {
            MessageBox.Show(this, "Bitte gib einen gültigen Geldbetrag ein.", "EasyCoop", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var message = $"Grundwerte von {savegame.SlotName} speichern?\n\nVor der Änderung wird automatisch ein vollständiges Backup erstellt.";
        if (MessageBox.Show(this, message, "Spielstand bearbeiten", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            _service.UpdateBasicValues(_game.ShortName, savegame, new SavegameBasicValues
            {
                SavegameName = SavegameNameBox.Text,
                FarmName = FarmNameBox.Text,
                Money = money
            });
            RefreshData();
            StatusText.Text = $"Grundwerte von {savegame.SlotName} wurden gespeichert.";
        }
        catch (Exception exception) { ShowError(exception, "Grundwerte speichern"); }
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (SavegamesGrid.SelectedItem is not SavegameInfo savegame || BackupsGrid.SelectedItem is not SavegameBackupInfo backup) { ShowSelectionHint(true); return; }
        var message = $"{savegame.SlotName} wird durch das Backup '{backup.Name}' ersetzt.\n\nDer aktuelle Stand wird vorher automatisch gesichert. Fortfahren?";
        if (MessageBox.Show(this, message, "Spielstand wiederherstellen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            _service.Restore(_game.ShortName, savegame, backup);
            RefreshData();
            StatusText.Text = $"{savegame.SlotName} wurde erfolgreich wiederhergestellt.";
        }
        catch (Exception exception) { ShowError(exception, "Wiederherstellung"); }
    }

    private void ShowSelectionHint(bool backup = false) => MessageBox.Show(this,
        backup ? "Bitte wähle einen Spielstand und ein Backup aus." : "Bitte wähle einen Spielstand aus.",
        "EasyCoop", MessageBoxButton.OK, MessageBoxImage.Information);

    private void ShowError(Exception exception, string context)
    {
        AppLogger.Error(exception, context);
        StatusText.Text = exception.Message;
        MessageBox.Show(this, exception.Message, "EasyCoop Spielstände", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

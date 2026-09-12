using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using EasyCoop.ModManager.Models;
using EasyCoop.ModManager.Services;

namespace EasyCoop.ModManager;

public partial class DedicatedServersWindow : Window
{
    private readonly GameInstallation _game;
    private readonly ModOperationService _operations;
    private readonly AppSettingsService _settingsService = new();
    private readonly ModScannerService _scanner = new();
    private readonly ServerComparisonService _comparison = new();
    private readonly ObservableCollection<ServerModComparison> _rows = new();
    private PortalApiClient? _client;

    public DedicatedServersWindow(GameInstallation game, ModOperationService operations)
    {
        InitializeComponent();
        _game = game;
        _operations = operations;
        ServerUrlBox.Text = _settingsService.Load().PortalApiBaseUrl;
        ComparisonGrid.ItemsSource = _rows;
        Closed += (_, _) => _client?.Dispose();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UsernameBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password))
        { ShowError("Bitte Benutzername und Passwort eingeben."); return; }
        try
        {
            SetBusy(true, "Verbindung mit der Serververwaltung …");
            _client?.Dispose();
            _client = new PortalApiClient(ServerUrlBox.Text);
            var login = await _client.LoginAsync(UsernameBox.Text.Trim(), PasswordBox.Password);
            PasswordBox.Clear();
            _settingsService.Save(new AppSettings { PortalApiBaseUrl = ServerUrlBox.Text.Trim() });
            var servers = (await _client.GetServersAsync()).Where(s => s.GameVersion == _game.ShortName).ToList();
            ServersBox.ItemsSource = servers;
            if (servers.Count > 0) ServersBox.SelectedIndex = 0;
            ActionStatus.Text = $"Angemeldet als {login.User.DisplayName} ({login.User.Role})";
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { SetBusy(false); }
    }

    private async void ServersBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => await RefreshServerAsync();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshServerAsync();

    private async Task RefreshServerAsync()
    {
        if (_client is null || ServersBox.SelectedItem is not ManagedServer server) return;
        try
        {
            SetBusy(true, "Serverdaten werden abgeglichen …");
            var status = await _client.GetServerStatusAsync(server);
            var serverMods = await _client.GetServerModsAsync(server);
            var localMods = await _scanner.ScanAsync(_game.ModsPath);
            var rows = _comparison.Compare(serverMods, localMods);
            _rows.Clear();
            foreach (var row in rows) _rows.Add(row);
            ServerStatusText.Text = $"{status.StatusText} · {status.Map} · {status.Players}/{status.MaxPlayers} Spieler · {serverMods.Count} Mods";
            ActionStatus.Text = $"{rows.Count(r => r.State != ServerModState.Current && r.State != ServerModState.LocalOnly)} Abweichungen";
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { SetBusy(false); }
    }

    private async void DownloadSelected_Click(object sender, RoutedEventArgs e)
    {
        if (ComparisonGrid.SelectedItem is not ServerModComparison row || row.ServerMod is null)
        { ShowError("Bitte eine Server-Mod auswählen."); return; }
        try
        {
            SetBusy(true, $"Lade {row.ServerMod.Title} …");
            await DownloadAndInstallAsync(row.ServerMod);
            await RefreshServerAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { SetBusy(false); }
    }

    private async void SyncAll_Click(object sender, RoutedEventArgs e)
    {
        var targets = _rows.Where(r => r.ServerMod is not null &&
            r.State is ServerModState.MissingLocally or ServerModState.Different).ToList();
        if (targets.Count == 0) { ShowInfo("Alle benötigten Server-Mods sind bereits aktuell."); return; }
        if (!Confirm($"{targets.Count} fehlende oder abweichende Mod(s) herunterladen und installieren?")) return;
        try
        {
            SetBusy(true);
            foreach (var row in targets) await DownloadAndInstallAsync(row.ServerMod!);
            await RefreshServerAsync();
            ShowInfo("Die Server-Mods wurden synchronisiert. Lokale Zusatzmods wurden nicht entfernt.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { SetBusy(false); }
    }

    private async Task DownloadAndInstallAsync(ServerMod mod)
    {
        if (_client is null || ServersBox.SelectedItem is not ManagedServer server)
            throw new InvalidOperationException("Keine Serververbindung aktiv.");
        string? temporary = null;
        try
        {
            ActionStatus.Text = $"Lade {mod.Title} …";
            temporary = await _client.DownloadServerModAsync(server, mod);
            await _operations.InstallAsync(temporary, _game.ModsPath);
        }
        finally
        {
            if (temporary is not null && File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private async void Upload_Click(object sender, RoutedEventArgs e)
    {
        if (_client is null || ServersBox.SelectedItem is not ManagedServer server)
        { ShowError("Bitte zuerst einen Server auswählen."); return; }
        if (ComparisonGrid.SelectedItem is not ServerModComparison row || row.LocalMod is null)
        { ShowError("Bitte eine lokal vorhandene Mod auswählen."); return; }
        if (!Confirm($"„{row.LocalMod.Title}“ auf „{server.Name}“ hochladen? Die API prüft dafür deine Rolle.")) return;
        try
        {
            SetBusy(true, "Mod wird sicher hochgeladen …");
            await _client.UploadServerModAsync(server, row.LocalMod);
            await RefreshServerAsync();
            ShowInfo("Die Mod wurde hochgeladen und serverseitig zur Prüfung übergeben.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { SetBusy(false); }
    }

    private async void Restart_Click(object sender, RoutedEventArgs e)
    {
        if (_client is null || ServersBox.SelectedItem is not ManagedServer server)
        { ShowError("Bitte zuerst einen Server auswählen."); return; }
        if (!Confirm($"Dedicated Server „{server.Name}“ wirklich neu starten? Aktive Spieler können getrennt werden.")) return;
        try { await _client.RestartServerAsync(server); ActionStatus.Text = "Neustart wurde angefordert."; }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void SetBusy(bool busy, string? message = null)
    {
        LoginButton.IsEnabled = !busy;
        ComparisonGrid.IsEnabled = !busy;
        if (message is not null) ActionStatus.Text = message;
    }

    private bool Confirm(string message) => MessageBox.Show(this, message, "EasyCoop Serververwaltung",
        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    private void ShowInfo(string message) => MessageBox.Show(this, message, "EasyCoop Serververwaltung",
        MessageBoxButton.OK, MessageBoxImage.Information);
    private void ShowError(string message)
    {
        AppLogger.Error(message, "Serververwaltung");
        ActionStatus.Text = message;
        MessageBox.Show(this, message, "EasyCoop Serververwaltung", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

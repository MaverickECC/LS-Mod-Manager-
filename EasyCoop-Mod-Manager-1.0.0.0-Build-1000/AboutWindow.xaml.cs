using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using EasyCoop.ModManager.Models;
using EasyCoop.ModManager.Services;

namespace EasyCoop.ModManager;

public partial class AboutWindow : Window
{
    private readonly AppSettings _settings;
    private readonly IReadOnlyList<GameInstallation> _games;

    public AboutWindow()
    {
        InitializeComponent();
        _settings = new AppSettingsService().Load();
        _games = GameDetectionService.Detect(_settings);
        Populate();
    }

    private void Populate()
    {
        VersionText.Text = $"Version {BuildInfo.Version} · Build {BuildInfo.BuildNumber}";
        ReleaseText.Text = $"Veröffentlicht am {BuildInfo.ReleaseDate}";
        ChannelText.Text = $"Update-Kanal: {_settings.Preferences.UpdateChannel}";
        FooterVersionText.Text = $"Programmversion {BuildInfo.Version}";
        DatabaseVersionText.Text = $"Mod-Datenbank {BuildInfo.ModDatabaseVersion}";
        SystemText.Text = BuildSystemText();
        GamesText.Text = BuildGamesText();
        WebsiteButton.IsEnabled = Uri.TryCreate(BuildInfo.WebsiteUrl, UriKind.Absolute, out _);
        DiscordButton.IsEnabled = Uri.TryCreate(BuildInfo.DiscordUrl, UriKind.Absolute, out _);
        ContactText.Text = $"Website: {Display(BuildInfo.WebsiteUrl)}\nDiscord: {Display(BuildInfo.DiscordUrl)}\nE-Mail: {Display(BuildInfo.SupportEmail)}";
    }

    private static string BuildSystemText()
    {
        var executable = Environment.ProcessPath ?? AppContext.BaseDirectory;
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var installation = executable.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase) ? "Systeminstallation" : "Benutzerinstallation";
        var architecture = Environment.Is64BitProcess ? $"64-Bit ({RuntimeInformation.ProcessArchitecture})" : $"32-Bit ({RuntimeInformation.ProcessArchitecture})";
        return $"Windows: {RuntimeInformation.OSDescription}\nArchitektur: {architecture}\nInstallation: {installation}\nLaufzeit: .NET {Environment.Version}\nProgrammordner: {AppContext.BaseDirectory}";
    }

    private string BuildGamesText() => string.Join("\n\n", _games.Select(game =>
    {
        var executable = _settings.GameLaunchSettings.GetValueOrDefault(game.ShortName)?.ExecutablePath;
        var install = string.IsNullOrWhiteSpace(executable) ? "nicht konfiguriert" : executable;
        var detected = game.IsAvailable || (!string.IsNullOrWhiteSpace(executable) && File.Exists(executable));
        return $"{game.Name}: {(detected ? "erkannt" : "nicht erkannt")}\nInstallation: {install}\nMods: {game.ModsPath}";
    }));

    private string DiagnosticsText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("EasyCoop Mod Manager – Diagnose");
        builder.AppendLine($"Version: {BuildInfo.Version} / Build {BuildInfo.BuildNumber}");
        builder.AppendLine($"Mod-Datenbank: {BuildInfo.ModDatabaseVersion}");
        builder.AppendLine($"Update-Kanal: {_settings.Preferences.UpdateChannel}");
        builder.AppendLine(BuildSystemText());
        builder.AppendLine(); builder.AppendLine(BuildGamesText());
        return builder.ToString();
    }

    private void CopyDiagnostics_Click(object sender, RoutedEventArgs e) { Clipboard.SetText(DiagnosticsText()); MessageBox.Show(this, "Die Diagnosedaten wurden in die Zwischenablage kopiert.", "EasyCoop Mod Manager"); }
    private void ReportError_Click(object sender, RoutedEventArgs e)
    {
        var subject = Uri.EscapeDataString($"EasyCoop Fehlerbericht – Version {BuildInfo.Version}");
        var body = Uri.EscapeDataString("Bitte beschreibe den Fehler oberhalb der folgenden Diagnosedaten:\n\n" + DiagnosticsText());
        Process.Start(new ProcessStartInfo($"mailto:{BuildInfo.SupportEmail}?subject={subject}&body={body}") { UseShellExecute = true });
    }
    private void Website_Click(object sender, RoutedEventArgs e) => Open(BuildInfo.WebsiteUrl);
    private void Discord_Click(object sender, RoutedEventArgs e) => Open(BuildInfo.DiscordUrl);
    private void Changelog_Click(object sender, RoutedEventArgs e) => MessageBox.Show(this, "Version 1.0.0.0 · Build 1000\n• Offizielle Erstveröffentlichung\n• Vollständige Modverwaltung für LS19, LS22 und LS25\n• Live-System- und Spieleinformationen\n• Kopierbare Diagnosedaten\n• Rechtliche und Supportinformationen\n\nDas vollständige Änderungsprotokoll steht in der README-Datei.", "Änderungsprotokoll");
    private void Licenses_Click(object sender, RoutedEventArgs e) => MessageBox.Show(this, "EasyCoop Mod Manager: Alle Rechte vorbehalten.\n\nLaufzeit: Microsoft .NET 8 und WPF – Lizenzbedingungen von Microsoft.\nInstaller: Inno Setup – Lizenzbedingungen von Jordan Russell und Martijn Laan.\n\nDas Projekt enthält derzeit keine zusätzlichen NuGet-Drittanbieterbibliotheken.", "Lizenzinformationen");
    private void Privacy_Click(object sender, RoutedEventArgs e) => MessageBox.Show(this, "EasyCoop speichert Einstellungen, Profile und Protokolle lokal im Windows-Benutzerkonto. Diagnosepakete werden nur auf ausdrückliche Benutzeraktion erstellt. Zugangstoken werden nicht in Diagnosedaten aufgenommen. Onlinefunktionen übertragen Daten erst bei ihrer bewussten Verwendung an den konfigurierten Dienst.", "Datenschutz");

    private static string Display(string value) => string.IsNullOrWhiteSpace(value) ? "noch nicht konfiguriert" : value;
    private static void Open(string target) { if (Uri.TryCreate(target, UriKind.Absolute, out _)) Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
}

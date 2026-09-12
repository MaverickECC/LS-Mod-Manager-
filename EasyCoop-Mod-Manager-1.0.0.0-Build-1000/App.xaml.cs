using System.Windows;

namespace EasyCoop.ModManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        EasyCoop.ModManager.Services.AppLogger.Information($"EasyCoop Mod Manager {EasyCoop.ModManager.Services.BuildInfo.Version} gestartet.");
        DispatcherUnhandledException += (_, args) =>
        {
            EasyCoop.ModManager.Services.AppLogger.Error(args.Exception, "Unbehandelter UI-Fehler");
            MessageBox.Show("Ein unerwarteter Fehler wurde protokolliert. Über Diagnose kann ein Supportpaket erstellt werden.",
                "EasyCoop Mod Manager", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Current.Shutdown(-1);
        };
    }
}

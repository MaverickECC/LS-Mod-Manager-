using System.Windows;
using System.Windows.Controls;
using EasyCoop.ModManager.Models;
using EasyCoop.ModManager.Services;
using Microsoft.Win32;

namespace EasyCoop.ModManager;

public partial class GameLaunchDialog : Window
{
    public GameLaunchSettings Settings { get; }
    public bool StartRequested { get; private set; }

    public GameLaunchDialog(string game, GameLaunchSettings settings)
    {
        InitializeComponent();
        Settings = settings;
        Heading.Text = $"{game} starten";
        ExecutableBox.Text = settings.ExecutablePath;
        ArgumentsBox.Text = settings.Arguments;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Farming-Simulator-Programm auswählen", Filter = "Programme (*.exe)|*.exe" };
        if (dialog.ShowDialog(this) == true) ExecutableBox.Text = dialog.FileName;
    }
    private void Preset_Click(object sender, RoutedEventArgs e) => ArgumentsBox.Text = (sender as Button)?.Tag?.ToString() ?? "";
    private void Save_Click(object sender, RoutedEventArgs e) => Complete(false);
    private void Start_Click(object sender, RoutedEventArgs e) => Complete(true);
    private void Complete(bool start)
    {
        if (!File.Exists(ExecutableBox.Text.Trim()))
        {
            MessageBox.Show(this, "Bitte eine vorhandene FarmingSimulator-EXE auswählen.", "EasyCoop Mod Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Settings.ExecutablePath = Path.GetFullPath(ExecutableBox.Text.Trim());
        Settings.Arguments = ArgumentsBox.Text.Trim();
        StartRequested = start;
        DialogResult = true;
    }
}

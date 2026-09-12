using System.Windows;

namespace EasyCoop.ModManager;

public partial class ModFolderNameDialog : Window
{
    public string FolderName => NameBox.Text.Trim();
    public ModFolderNameDialog(string suggestedName)
    {
        InitializeComponent();
        NameBox.Text = suggestedName;
        Loaded += (_, _) => { NameBox.Focus(); NameBox.SelectAll(); };
    }
    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FolderName)) return;
        DialogResult = true;
    }
}

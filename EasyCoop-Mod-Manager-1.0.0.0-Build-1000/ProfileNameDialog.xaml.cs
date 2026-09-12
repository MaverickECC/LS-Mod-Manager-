using System.Windows;

namespace EasyCoop.ModManager;

public partial class ProfileNameDialog : Window
{
    public string ProfileName => NameBox.Text.Trim();

    public ProfileNameDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NameBox.Focus();
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            MessageBox.Show(this, "Bitte einen Profilnamen eingeben.", "EasyCoop Mod Manager",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DialogResult = true;
    }
}

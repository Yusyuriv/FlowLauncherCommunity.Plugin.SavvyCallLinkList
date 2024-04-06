using System.Windows;
using System.Windows.Controls;

namespace FlowLauncherCommunity.Plugin.SavvyCalLinkList;

public partial class SettingsView : UserControl {
    public Settings Settings { get; }
    
    public SettingsView(Settings settings) {
        Settings = settings;
        InitializeComponent();
        PasswordBox.Password = settings.Token;
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e) {
        if (sender is not PasswordBox passwordBox) return;
        Settings.Token = passwordBox.Password;
    }
}
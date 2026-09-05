using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PassVaultWindows.Views;

public partial class BiometricOptInView : UserControl
{
    private readonly AppState _appState;
    private readonly Func<bool, Task> _onFinish;

    public BiometricOptInView(AppState appState, Func<bool, Task> onFinish)
    {
        InitializeComponent();
        _appState = appState;
        _onFinish = onFinish;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        bool available = await _appState.WindowsHello.IsAvailableAsync();
        if (available)
        {
            TitleText.Text = "One last step";
            SubtitleText.Text = "Add Windows Hello (fingerprint or face) as an extra step before your pattern every time you open PassVault. You can turn this off later in Settings.";
            EnableButton.Visibility = Visibility.Visible;
            SkipButton.Visibility = Visibility.Visible;
        }
        else
        {
            TitleText.Text = "You're all set";
            SubtitleText.Text = "No Windows Hello was found on this PC, so you'll log in with your pattern. You can enable it later in Settings if you set one up.";
            FinishButton.Visibility = Visibility.Visible;
        }
    }

    private async void Enable_Click(object sender, RoutedEventArgs e) => await _onFinish(true);

    private async void Skip_Click(object sender, RoutedEventArgs e) => await _onFinish(false);

    private async void Finish_Click(object sender, RoutedEventArgs e) => await _onFinish(false);
}

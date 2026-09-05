using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PassVaultWindows.Views;

public partial class LoginView : UserControl
{
    private readonly AppState _appState;
    private readonly Action _onLoginSuccess;
    private readonly Action _onForgotPattern;
    private bool _isVerifyingPattern;

    public LoginView(AppState appState, Action onLoginSuccess, Action onForgotPattern)
    {
        InitializeComponent();
        _appState = appState;
        _onLoginSuccess = onLoginSuccess;
        _onForgotPattern = onForgotPattern;
        PatternControl.PatternCompleted += OnPatternCompleted;

        _ = StartAsync();
    }

    private async Task StartAsync()
    {
        bool useHelloFirst = _appState.IsWindowsHelloEnabled && await _appState.WindowsHello.IsAvailableAsync();
        if (useHelloFirst)
        {
            StatusText.Text = "Verifying...";
            HelloPanel.Visibility = Visibility.Visible;
            await TryHelloAsync();
        }
        else
        {
            ShowPatternStep("Draw your pattern");
        }
    }

    private async Task TryHelloAsync()
    {
        HelloPanel.Visibility = Visibility.Collapsed;
        VerifyingProgress.Visibility = Visibility.Visible;
        bool success = await _appState.WindowsHello.AuthenticateAsync("Unlock PassVault");
        VerifyingProgress.Visibility = Visibility.Collapsed;
        if (success)
        {
            ShowPatternStep("Now draw your pattern");
        }
        else
        {
            _appState.RecordFailedWindowsHelloAttempt();
            HelloPanel.Visibility = Visibility.Visible;
            StatusText.Text = "Windows Hello didn't verify you";
        }
    }

    private async void RetryHello_Click(object sender, RoutedEventArgs e) => await TryHelloAsync();

    private void ShowPatternStep(string status)
    {
        StatusText.Text = status;
        HelloPanel.Visibility = Visibility.Collapsed;
        PatternPanel.Visibility = Visibility.Visible;
    }

    private async void OnPatternCompleted(List<int> pattern)
    {
        if (_isVerifyingPattern)
        {
            return;
        }
        _isVerifyingPattern = true;
        StatusText.Text = "Verifying...";
        PatternPanel.Visibility = Visibility.Collapsed;
        VerifyingProgress.Visibility = Visibility.Visible;

        bool success = await _appState.TryLoginWithPatternAsync(pattern);

        VerifyingProgress.Visibility = Visibility.Collapsed;
        _isVerifyingPattern = false;
        if (success)
        {
            _onLoginSuccess();
        }
        else
        {
            StatusText.Text = "Wrong pattern, try again";
            PatternPanel.Visibility = Visibility.Visible;
            PatternControl.ShowError = true;
            await Task.Delay(500);
            PatternControl.ShowError = false;
        }
    }

    private void ForgotPattern_Click(object sender, RoutedEventArgs e) => _onForgotPattern();
}

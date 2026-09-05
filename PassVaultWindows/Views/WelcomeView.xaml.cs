using System;
using System.Windows;
using System.Windows.Controls;

namespace PassVaultWindows.Views;

public partial class WelcomeView : UserControl
{
    private readonly Action _onGetStarted;

    public WelcomeView(Action onGetStarted)
    {
        InitializeComponent();
        _onGetStarted = onGetStarted;
    }

    private void GetStarted_Click(object sender, RoutedEventArgs e) => _onGetStarted();
}

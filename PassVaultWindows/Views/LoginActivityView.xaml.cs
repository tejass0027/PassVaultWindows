using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PassVaultWindows.Auth;

namespace PassVaultWindows.Views;

public partial class LoginActivityView : UserControl
{
    private readonly Action _onBack;

    public LoginActivityView(List<LoginEvent> events, Action onBack)
    {
        InitializeComponent();
        _onBack = onBack;

        if (events.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
        }
        else
        {
            EventsList.ItemsSource = events.Select(evt => new EventItem(evt)).ToList();
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e) => _onBack();

    private class EventItem
    {
        public string Label { get; }
        public string TimeText { get; }
        public Brush IndicatorBrush { get; }
        public Brush TextBrush { get; }

        public EventItem(LoginEvent evt)
        {
            string kind = evt.Type switch
            {
                LoginEventType.Pattern => "Pattern",
                LoginEventType.WindowsHello => "Windows Hello",
                LoginEventType.Recovery => "Security question recovery",
                _ => "Unknown"
            };
            Label = evt.Success ? $"{kind} unlock" : $"Incorrect {kind} attempt";
            TimeText = DateTimeOffset.FromUnixTimeMilliseconds(evt.Timestamp).ToLocalTime().ToString("MMM d, yyyy · h:mm tt");

            IndicatorBrush = evt.Success
                ? (Brush)Application.Current.Resources["SuccessBrush"]
                : (Brush)Application.Current.Resources["ErrorBrush"];
            TextBrush = evt.Success
                ? (Brush)Application.Current.Resources["TextPrimaryBrush"]
                : (Brush)Application.Current.Resources["ErrorBrush"];
        }
    }
}

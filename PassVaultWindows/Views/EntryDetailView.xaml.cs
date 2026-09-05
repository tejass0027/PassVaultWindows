using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PassVaultWindows.Data;

namespace PassVaultWindows.Views;

public partial class EntryDetailView : UserControl
{
    private readonly AppState _appState;
    private readonly Credential _credential;
    private readonly Action _onBack;
    private readonly Action _onEdit;
    private readonly Action _onDeleted;
    private bool _passwordVisible;
    private DispatcherTimer? _clipboardClearTimer;
    private string? _expectedClipboardValue;

    public EntryDetailView(AppState appState, Credential credential, Action onBack, Action onEdit, Action onDeleted)
    {
        InitializeComponent();
        _appState = appState;
        _credential = credential;
        _onBack = onBack;
        _onEdit = onEdit;
        _onDeleted = onDeleted;

        TitleText.Text = string.IsNullOrEmpty(credential.Title) ? "(untitled)" : credential.Title;
        UsernameText.Text = credential.Username;
        UpdatePasswordDisplay();

        if (!string.IsNullOrEmpty(credential.Url))
        {
            UrlPanel.Visibility = Visibility.Visible;
            UrlText.Text = credential.Url;
        }
        if (!string.IsNullOrEmpty(credential.Notes))
        {
            NotesPanel.Visibility = Visibility.Visible;
            NotesText.Text = credential.Notes;
        }
    }

    private void UpdatePasswordDisplay()
    {
        PasswordText.Text = _passwordVisible
            ? _credential.Password
            : new string('•', Math.Min(_credential.Password.Length, 16));
        ToggleShowButton.Content = _passwordVisible ? "Hide" : "Show";
    }

    private void ToggleShow_Click(object sender, RoutedEventArgs e)
    {
        _passwordVisible = !_passwordVisible;
        UpdatePasswordDisplay();
    }

    private void CopyUsername_Click(object sender, RoutedEventArgs e) => CopyWithAutoClear(_credential.Username);

    private void CopyPassword_Click(object sender, RoutedEventArgs e) => CopyWithAutoClear(_credential.Password);

    private void CopyUrl_Click(object sender, RoutedEventArgs e) => CopyWithAutoClear(_credential.Url);

    private void CopyWithAutoClear(string value)
    {
        Clipboard.SetText(value);
        _expectedClipboardValue = value;

        _clipboardClearTimer?.Stop();
        _clipboardClearTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _clipboardClearTimer.Tick += (_, _) =>
        {
            _clipboardClearTimer!.Stop();
            try
            {
                if (Clipboard.ContainsText() && Clipboard.GetText() == _expectedClipboardValue)
                {
                    Clipboard.Clear();
                }
            }
            catch (Exception)
            {
                // Clipboard can be locked by another process; not worth surfacing an error for this.
            }
        };
        _clipboardClearTimer.Start();
    }

    private void Back_Click(object sender, RoutedEventArgs e) => _onBack();

    private void Edit_Click(object sender, RoutedEventArgs e) => _onEdit();

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            $"\"{TitleText.Text}\" will be permanently deleted.",
            "Delete this entry?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            await _appState.VaultRepository.DeleteAsync(_credential.Id);
            _onDeleted();
        }
    }
}

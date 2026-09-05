using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PassVaultWindows.Data;

namespace PassVaultWindows.Views;

public partial class BackupExportView : UserControl
{
    private readonly AppState _appState;
    private readonly Action _onDone;
    private readonly Action _onCancel;

    public BackupExportView(AppState appState, Action onDone, Action onCancel)
    {
        InitializeComponent();
        _appState = appState;
        _onDone = onDone;
        _onCancel = onCancel;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var password = PasswordBox.Text;
        var confirm = ConfirmPasswordBox.Text;

        if (password.Length < 6)
        {
            ShowError("Use a password of at least 6 characters");
            return;
        }
        if (password != confirm)
        {
            ShowError("Passwords don't match");
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = "passvault-backup.pvbk",
            Filter = "PassVault backup (*.pvbk)|*.pvbk|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        StatusText.Visibility = Visibility.Collapsed;
        SaveButton.IsEnabled = false;
        Progress.Visibility = Visibility.Visible;

        try
        {
            var credentials = _appState.VaultRepository.Credentials;
            await Task.Run(() =>
            {
                using var stream = File.Create(dialog.FileName);
                BackupManager.Export(stream, credentials, password);
            });
            StatusText.Text = "Backup saved.";
            StatusText.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ShowError($"Couldn't write the backup file: {ex.Message}");
        }
        finally
        {
            SaveButton.IsEnabled = true;
            Progress.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void Done_Click(object sender, RoutedEventArgs e) => _onDone();

    private void Cancel_Click(object sender, RoutedEventArgs e) => _onCancel();
}

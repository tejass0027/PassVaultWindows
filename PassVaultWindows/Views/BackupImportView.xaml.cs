using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PassVaultWindows.Data;

namespace PassVaultWindows.Views;

public partial class BackupImportView : UserControl
{
    private readonly AppState _appState;
    private readonly Action _onDone;
    private readonly Action _onCancel;

    public BackupImportView(AppState appState, Action onDone, Action onCancel)
    {
        InitializeComponent();
        _appState = appState;
        _onDone = onDone;
        _onCancel = onCancel;
    }

    private async void ChooseFile_Click(object sender, RoutedEventArgs e)
    {
        var password = PasswordBox.Text;
        if (string.IsNullOrEmpty(password))
        {
            ShowError("Enter the backup password first");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "PassVault backup (*.pvbk)|*.pvbk|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        ChooseFileButton.IsEnabled = false;
        Progress.Visibility = Visibility.Visible;

        List<Credential>? imported = null;
        try
        {
            imported = await Task.Run(() =>
            {
                using var stream = File.OpenRead(dialog.FileName);
                return BackupManager.Import(stream, password);
            });
        }
        catch (Exception ex)
        {
            ShowError($"Couldn't read that file: {ex.Message}");
        }
        finally
        {
            ChooseFileButton.IsEnabled = true;
            Progress.Visibility = Visibility.Collapsed;
        }

        if (imported == null)
        {
            ShowError("Wrong password, or this isn't a valid PassVault backup file.");
            return;
        }

        var result = MessageBox.Show(
            $"This backup contains {imported.Count} saved password(s). Importing will replace everything currently in your vault on this PC.",
            "Replace current vault?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            await _appState.VaultRepository.ReplaceAllAsync(imported);
            _onDone();
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => _onCancel();
}

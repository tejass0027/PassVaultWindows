using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PassVaultWindows.Data;

namespace PassVaultWindows.Views;

public partial class VaultListView : UserControl
{
    private readonly AppState _appState;
    private readonly Action _onAddEntry;
    private readonly Action<Credential> _onOpenEntry;
    private readonly Action _onOpenSettings;
    private readonly Action _onOpenPhotoVault;
    private readonly Action _onViewLoginActivity;
    private readonly Action _onLock;

    public VaultListView(
        AppState appState,
        Action onAddEntry,
        Action<Credential> onOpenEntry,
        Action onOpenSettings,
        Action onOpenPhotoVault,
        Action onViewLoginActivity,
        Action onLock)
    {
        InitializeComponent();
        _appState = appState;
        _onAddEntry = onAddEntry;
        _onOpenEntry = onOpenEntry;
        _onOpenSettings = onOpenSettings;
        _onOpenPhotoVault = onOpenPhotoVault;
        _onViewLoginActivity = onViewLoginActivity;
        _onLock = onLock;

        _appState.VaultRepository.CredentialsChanged += RefreshList;
        _appState.FailedAttemptsSinceLastLoginChanged += _ => RefreshBanner();

        RefreshBanner();
        RefreshList();
    }

    private void RefreshBanner()
    {
        int count = _appState.FailedAttemptsSinceLastLogin;
        if (count <= 0)
        {
            BannerBorder.Visibility = Visibility.Collapsed;
            return;
        }
        BannerBorder.Visibility = Visibility.Visible;
        BannerTitleText.Text = count == 1 ? "Someone tried to open PassVault" : $"Someone tried to open PassVault {count} times";
        BannerSubtitleText.Text = $"{count} incorrect attempt{(count == 1 ? "" : "s")} before you logged in";
    }

    private void DismissBanner_Click(object sender, RoutedEventArgs e)
    {
        _appState.DismissFailedAttemptsBanner();
        RefreshBanner();
    }

    private void ViewActivity_Click(object sender, RoutedEventArgs e) => _onViewLoginActivity();

    private void RefreshList()
    {
        var query = SearchBox.Text?.Trim() ?? "";
        var all = _appState.VaultRepository.Credentials;
        var filtered = all
            .Where(c => string.IsNullOrEmpty(query) ||
                        c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        c.Username.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Title.ToLowerInvariant())
            .Select(c => new CredentialListItem(c))
            .ToList();

        CredentialsList.ItemsSource = filtered;

        if (filtered.Count == 0)
        {
            EmptyText.Text = all.Count > 0 ? "No matches" : "No saved passwords yet. Tap + to add one.";
            EmptyText.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyText.Visibility = Visibility.Collapsed;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        RefreshList();
    }

    private void CredentialsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CredentialsList.SelectedItem is CredentialListItem item)
        {
            _onOpenEntry(item.Credential);
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e) => _onAddEntry();

    private void Settings_Click(object sender, RoutedEventArgs e) => _onOpenSettings();

    private void PhotoVault_Click(object sender, RoutedEventArgs e) => _onOpenPhotoVault();

    private void Lock_Click(object sender, RoutedEventArgs e) => _onLock();

    private class CredentialListItem
    {
        public Credential Credential { get; }
        public string Title => string.IsNullOrEmpty(Credential.Title) ? "(untitled)" : Credential.Title;
        public string Username => Credential.Username;

        public CredentialListItem(Credential credential)
        {
            Credential = credential;
        }
    }
}

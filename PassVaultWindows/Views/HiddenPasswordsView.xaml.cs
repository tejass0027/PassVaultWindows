using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PassVaultWindows.Data;

namespace PassVaultWindows.Views;

/// <summary>
/// The hidden vault's own password list. Opens the same detail and add/edit screens as the main
/// vault, just fed from the hidden vault's separately-encrypted store.
/// </summary>
public partial class HiddenPasswordsView : UserControl
{
    private readonly AppState _appState;
    private readonly Action _onAdd;
    private readonly Action<Credential> _onOpen;
    private readonly Action _onBack;

    public HiddenPasswordsView(AppState appState, Action onAdd, Action<Credential> onOpen, Action onBack)
    {
        InitializeComponent();
        _appState = appState;
        _onAdd = onAdd;
        _onOpen = onOpen;
        _onBack = onBack;

        _appState.HiddenVaultRepository.CredentialsChanged += RefreshList;
        RefreshList();
    }

    private void RefreshList()
    {
        var items = _appState.HiddenVaultRepository.Credentials
            .OrderBy(c => c.Title.ToLowerInvariant())
            .Select(c => new CredentialListItem(c))
            .ToList();
        CredentialsList.ItemsSource = items;
        EmptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CredentialsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CredentialsList.SelectedItem is CredentialListItem item)
        {
            CredentialsList.SelectedItem = null;
            _onOpen(item.Credential);
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e) => _onAdd();

    private void Back_Click(object sender, RoutedEventArgs e) => _onBack();

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

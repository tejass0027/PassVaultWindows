using System;
using System.Windows;
using System.Windows.Controls;

namespace PassVaultWindows.Views;

/// <summary>
/// The hidden vault's home screen: a small hub leading to its own secret passwords, photos and
/// notes. Reachable only by drawing the hidden vault's pattern on the login screen.
/// </summary>
public partial class HiddenVaultHubView : UserControl
{
    private readonly Action _onOpenPasswords;
    private readonly Action _onOpenPhotos;
    private readonly Action _onOpenNotes;
    private readonly Action _onLock;

    public HiddenVaultHubView(Action onOpenPasswords, Action onOpenPhotos, Action onOpenNotes, Action onLock)
    {
        InitializeComponent();
        _onOpenPasswords = onOpenPasswords;
        _onOpenPhotos = onOpenPhotos;
        _onOpenNotes = onOpenNotes;
        _onLock = onLock;
    }

    private void Passwords_Click(object sender, RoutedEventArgs e) => _onOpenPasswords();

    private void Photos_Click(object sender, RoutedEventArgs e) => _onOpenPhotos();

    private void Notes_Click(object sender, RoutedEventArgs e) => _onOpenNotes();

    private void Lock_Click(object sender, RoutedEventArgs e) => _onLock();
}

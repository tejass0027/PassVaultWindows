using System;
using System.Windows;
using System.Windows.Controls;

namespace PassVaultWindows.Controls;

/// <summary>A text field masked like a password by default, with a Show/Hide toggle button.</summary>
public partial class PasswordRevealBox : UserControl
{
    private bool _isRevealed;
    private bool _suppressSync;

    public PasswordRevealBox()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => _isRevealed ? RevealedBox.Text : MaskedBox.Password;
        set
        {
            _suppressSync = true;
            MaskedBox.Password = value;
            RevealedBox.Text = value;
            _suppressSync = false;
        }
    }

    public event EventHandler? TextChanged;

    private void MaskedBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressSync)
        {
            return;
        }
        _suppressSync = true;
        RevealedBox.Text = MaskedBox.Password;
        _suppressSync = false;
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RevealedBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressSync)
        {
            return;
        }
        _suppressSync = true;
        MaskedBox.Password = RevealedBox.Text;
        _suppressSync = false;
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isRevealed = !_isRevealed;
        MaskedBox.Visibility = _isRevealed ? Visibility.Collapsed : Visibility.Visible;
        RevealedBox.Visibility = _isRevealed ? Visibility.Visible : Visibility.Collapsed;
        ToggleButton.Content = _isRevealed ? "Hide" : "Show";
        if (_isRevealed)
        {
            RevealedBox.Focus();
            RevealedBox.CaretIndex = RevealedBox.Text.Length;
        }
        else
        {
            MaskedBox.Focus();
        }
    }
}

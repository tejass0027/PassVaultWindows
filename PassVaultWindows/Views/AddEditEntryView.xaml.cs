using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PassVaultWindows.Data;

namespace PassVaultWindows.Views;

public partial class AddEditEntryView : UserControl
{
    private readonly Credential? _existing;
    private readonly Func<Credential, Task> _onSave;
    private readonly Action _onBack;

    public AddEditEntryView(Credential? existing, Func<Credential, Task> onSave, Action onBack)
    {
        InitializeComponent();
        _existing = existing;
        _onSave = onSave;
        _onBack = onBack;

        TitleBarText.Text = existing == null ? "Add password" : "Edit password";
        TitleBox.Text = existing?.Title ?? "";
        UsernameBox.Text = existing?.Username ?? "";
        PasswordBox.Text = existing?.Password ?? "";
        UrlBox.Text = existing?.Url ?? "";
        NotesBox.Text = existing?.Notes ?? "";

        PasswordBox.TextChanged += (_, _) => UpdateStrength();
        UpdateStrength();
    }

    private void UpdateStrength()
    {
        var password = PasswordBox.Text;
        int score = StrengthScore(password);
        StrengthBar.Value = score;

        if (string.IsNullOrEmpty(password))
        {
            StrengthLabel.Text = "";
            StrengthBar.Foreground = (Brush)FindResource("OutlineBrush");
        }
        else if (score <= 2)
        {
            StrengthLabel.Text = "Weak";
            StrengthLabel.Foreground = Brushes.Crimson;
            StrengthBar.Foreground = Brushes.Crimson;
        }
        else if (score <= 4)
        {
            StrengthLabel.Text = "Okay";
            StrengthLabel.Foreground = Brushes.DarkOrange;
            StrengthBar.Foreground = Brushes.DarkOrange;
        }
        else
        {
            StrengthLabel.Text = "Strong";
            StrengthLabel.Foreground = (Brush)FindResource("SuccessBrush");
            StrengthBar.Foreground = (Brush)FindResource("SuccessBrush");
        }
    }

    private static int StrengthScore(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return 0;
        }
        int score = 0;
        if (password.Length >= 8)
        {
            score++;
        }
        if (password.Length >= 12)
        {
            score++;
        }
        if (password.Any(char.IsUpper))
        {
            score++;
        }
        if (password.Any(char.IsLower))
        {
            score++;
        }
        if (password.Any(char.IsDigit))
        {
            score++;
        }
        if (password.Any(c => !char.IsLetterOrDigit(c)))
        {
            score++;
        }
        return score;
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PasswordGeneratorWindow { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.GeneratedPassword != null)
        {
            PasswordBox.Text = dialog.GeneratedPassword;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Text))
        {
            MessageBox.Show("Title and password are required.", "Missing info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var credential = new Credential
        {
            Id = _existing?.Id ?? Guid.NewGuid().ToString(),
            Title = TitleBox.Text,
            Username = UsernameBox.Text,
            Password = PasswordBox.Text,
            Url = UrlBox.Text,
            Notes = NotesBox.Text,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await _onSave(credential);
    }

    private void Back_Click(object sender, RoutedEventArgs e) => _onBack();
}

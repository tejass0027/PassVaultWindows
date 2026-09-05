using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace PassVaultWindows.Views;

public partial class PasswordGeneratorWindow : Window
{
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{}?";

    public string? GeneratedPassword { get; private set; }

    public PasswordGeneratorWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Regenerate();
    }

    private void Option_Changed(object sender, RoutedEventArgs e) => Regenerate();

    private void Regenerate()
    {
        if (LengthText == null || GeneratedText == null)
        {
            return;
        }
        int length = (int)LengthSlider.Value;
        LengthText.Text = length.ToString();

        var pool = new StringBuilder(Lower);
        if (UpperCheck.IsChecked == true)
        {
            pool.Append(Upper);
        }
        if (DigitsCheck.IsChecked == true)
        {
            pool.Append(Digits);
        }
        if (SymbolsCheck.IsChecked == true)
        {
            pool.Append(Symbols);
        }

        var poolString = pool.ToString();
        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            result.Append(poolString[RandomNumberGenerator.GetInt32(poolString.Length)]);
        }
        GeneratedText.Text = result.ToString();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Use_Click(object sender, RoutedEventArgs e)
    {
        GeneratedPassword = GeneratedText.Text;
        DialogResult = true;
        Close();
    }
}

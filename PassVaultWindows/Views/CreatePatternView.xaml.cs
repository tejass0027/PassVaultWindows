using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using PassVaultWindows.Auth;

namespace PassVaultWindows.Views;

public partial class CreatePatternView : UserControl
{
    private readonly Func<List<int>, Task> _onPatternConfirmed;
    private List<int>? _firstPattern;

    public CreatePatternView(Func<List<int>, Task> onPatternConfirmed)
    {
        InitializeComponent();
        _onPatternConfirmed = onPatternConfirmed;
        PatternControl.PatternCompleted += OnPatternCompleted;
        UpdateText();
    }

    private async void OnPatternCompleted(List<int> pattern)
    {
        if (pattern.Count < PatternAuthManager.MinPatternLength)
        {
            await FlashError($"Connect at least {PatternAuthManager.MinPatternLength} dots");
            return;
        }

        if (_firstPattern == null)
        {
            _firstPattern = pattern;
            UpdateText();
        }
        else if (PatternsEqual(_firstPattern, pattern))
        {
            await _onPatternConfirmed(pattern);
        }
        else
        {
            _firstPattern = null;
            await FlashError("Patterns didn't match, try again");
        }
    }

    private void UpdateText()
    {
        if (_firstPattern == null)
        {
            TitleText.Text = "Draw a new pattern";
            SubtitleText.Text = $"Connect at least {PatternAuthManager.MinPatternLength} dots. You'll use this every time you log in.";
        }
        else
        {
            TitleText.Text = "Confirm your pattern";
            SubtitleText.Text = "Draw the same pattern again to confirm.";
        }
    }

    private async Task FlashError(string message)
    {
        SubtitleText.Text = message;
        PatternControl.ShowError = true;
        await Task.Delay(500);
        PatternControl.ShowError = false;
        UpdateText();
    }

    private static bool PatternsEqual(List<int> a, List<int> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }
        return true;
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PassVaultWindows.Auth;

namespace PassVaultWindows.Views;

public partial class ChangePatternView : UserControl
{
    private readonly AppState _appState;
    private readonly Action _onDone;
    private readonly Action _onCancel;
    private List<int>? _firstPattern;

    public ChangePatternView(AppState appState, Action onDone, Action onCancel)
    {
        InitializeComponent();
        _appState = appState;
        _onDone = onDone;
        _onCancel = onCancel;
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
            Progress.Visibility = Visibility.Visible;
            await _appState.ChangePatternAsync(pattern);
            Progress.Visibility = Visibility.Collapsed;
            _onDone();
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
            SubtitleText.Text = $"Connect at least {PatternAuthManager.MinPatternLength} dots";
        }
        else
        {
            TitleText.Text = "Confirm your new pattern";
            SubtitleText.Text = "Draw the same pattern again";
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

    private void Cancel_Click(object sender, RoutedEventArgs e) => _onCancel();
}

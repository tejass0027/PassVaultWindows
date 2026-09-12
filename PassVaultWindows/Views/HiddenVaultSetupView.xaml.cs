using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PassVaultWindows.Auth;

namespace PassVaultWindows.Views;

/// <summary>
/// Draws and confirms a pattern for the hidden vault. If one already exists, first requires
/// drawing the *current* hidden pattern (Settings runs inside the main vault's session, so the
/// hidden vault's DEK isn't otherwise available to rewrap under a new pattern).
/// </summary>
public partial class HiddenVaultSetupView : UserControl
{
    private enum Step { VerifyOld, DrawNew, ConfirmNew }

    private readonly AppState _appState;
    private readonly Action _onDone;
    private readonly Action _onCancel;
    private Step _step;
    private List<int>? _firstPattern;

    public HiddenVaultSetupView(AppState appState, Action onDone, Action onCancel)
    {
        InitializeComponent();
        _appState = appState;
        _onDone = onDone;
        _onCancel = onCancel;
        _step = _appState.HasHiddenVault ? Step.VerifyOld : Step.DrawNew;
        PatternControl.PatternCompleted += OnPatternCompleted;
        UpdateText();
    }

    private async void OnPatternCompleted(List<int> pattern)
    {
        switch (_step)
        {
            case Step.VerifyOld:
                Progress.Visibility = Visibility.Visible;
                var verified = await _appState.BeginHiddenVaultPatternChangeAsync(pattern);
                Progress.Visibility = Visibility.Collapsed;
                if (verified)
                {
                    _step = Step.DrawNew;
                    UpdateText();
                }
                else
                {
                    await FlashError("Wrong pattern, try again");
                }
                break;

            case Step.DrawNew:
                if (pattern.Count < PatternAuthManager.MinPatternLength)
                {
                    await FlashError($"Connect at least {PatternAuthManager.MinPatternLength} dots");
                    return;
                }
                _firstPattern = pattern;
                _step = Step.ConfirmNew;
                UpdateText();
                break;

            case Step.ConfirmNew:
                if (PatternsEqual(_firstPattern!, pattern))
                {
                    Progress.Visibility = Visibility.Visible;
                    var ok = await _appState.SetupHiddenVaultAsync(pattern);
                    Progress.Visibility = Visibility.Collapsed;
                    if (ok)
                    {
                        _onDone();
                    }
                    else
                    {
                        _firstPattern = null;
                        _step = Step.DrawNew;
                        await FlashError("That's your main pattern - choose a different one");
                    }
                }
                else
                {
                    _firstPattern = null;
                    _step = Step.DrawNew;
                    await FlashError("Patterns didn't match, try again");
                }
                break;
        }
    }

    private void UpdateText()
    {
        switch (_step)
        {
            case Step.VerifyOld:
                TitleText.Text = "Draw your current hidden vault pattern";
                SubtitleText.Text = "Prove you know it before changing it";
                break;
            case Step.DrawNew:
                TitleText.Text = "Draw a new hidden vault pattern";
                SubtitleText.Text = $"This must be different from your main pattern. Connect at least {PatternAuthManager.MinPatternLength} dots.";
                break;
            case Step.ConfirmNew:
                TitleText.Text = "Confirm your new hidden vault pattern";
                SubtitleText.Text = "Draw the same pattern again";
                break;
        }
    }

    private async Task FlashError(string message)
    {
        SubtitleText.Text = message;
        PatternControl.ShowError = true;
        await Task.Delay(700);
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

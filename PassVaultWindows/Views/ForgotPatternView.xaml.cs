using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PassVaultWindows.Auth;

namespace PassVaultWindows.Views;

public partial class ForgotPatternView : UserControl
{
    private enum Step { Answers, DrawNewPattern, ConfirmNewPattern }

    private readonly AppState _appState;
    private readonly Action _onRecovered;
    private readonly Action _onCancel;
    private readonly List<string> _questions;
    private List<string> _answers = new();
    private List<int>? _firstNewPattern;
    private Step _step = Step.Answers;

    public ForgotPatternView(AppState appState, Action onRecovered, Action onCancel)
    {
        InitializeComponent();
        _appState = appState;
        _onRecovered = onRecovered;
        _onCancel = onCancel;
        _questions = appState.SecurityQuestions.Questions();
        NewPatternControl.PatternCompleted += OnNewPatternCompleted;

        if (_questions.Count < 3)
        {
            NoQuestionsPanel.Visibility = Visibility.Visible;
        }
        else
        {
            Question1Label.Text = _questions[0];
            Question2Label.Text = _questions[1];
            Question3Label.Text = _questions[2];
            AnswersScroll.Visibility = Visibility.Visible;
        }
    }

    private void BackToLogin_Click(object sender, RoutedEventArgs e) => _onCancel();

    private void Cancel_Click(object sender, RoutedEventArgs e) => _onCancel();

    private async void Continue_Click(object sender, RoutedEventArgs e)
    {
        var a1 = Answer1Box.Text;
        var a2 = Answer2Box.Text;
        var a3 = Answer3Box.Text;
        if (string.IsNullOrWhiteSpace(a1) || string.IsNullOrWhiteSpace(a2) || string.IsNullOrWhiteSpace(a3))
        {
            ShowAnswersError("Please answer all questions");
            return;
        }

        ContinueButton.IsEnabled = false;
        AnswersProgress.Visibility = Visibility.Visible;
        var answers = new List<string> { a1, a2, a3 };
        bool correct = await _appState.VerifySecurityAnswersAsync(answers);
        AnswersProgress.Visibility = Visibility.Collapsed;
        ContinueButton.IsEnabled = true;

        if (!correct)
        {
            ShowAnswersError("Those answers didn't match our records");
            return;
        }

        _answers = answers;
        AnswersErrorText.Visibility = Visibility.Collapsed;
        _step = Step.DrawNewPattern;
        ShowPatternStep();
    }

    private void ShowAnswersError(string message)
    {
        AnswersErrorText.Text = message;
        AnswersErrorText.Visibility = Visibility.Visible;
    }

    private void ShowPatternStep()
    {
        AnswersScroll.Visibility = Visibility.Collapsed;
        PatternStepPanel.Visibility = Visibility.Visible;
        PatternStepTitle.Text = _step == Step.ConfirmNewPattern ? "Confirm your new pattern" : "Draw a new pattern";
    }

    private async void OnNewPatternCompleted(List<int> pattern)
    {
        if (pattern.Count < PatternAuthManager.MinPatternLength)
        {
            await FlashPatternError();
            return;
        }

        if (_step == Step.DrawNewPattern)
        {
            _firstNewPattern = pattern;
            _step = Step.ConfirmNewPattern;
            PatternStepTitle.Text = "Confirm your new pattern";
        }
        else if (PatternsEqual(pattern, _firstNewPattern))
        {
            PatternProgress.Visibility = Visibility.Visible;
            bool success = await _appState.RecoverWithSecurityAnswersAsync(_answers, pattern);
            PatternProgress.Visibility = Visibility.Collapsed;
            if (success)
            {
                _onRecovered();
            }
            else
            {
                // Answers somehow stopped matching between steps; start over.
                _step = Step.Answers;
                _firstNewPattern = null;
                PatternStepPanel.Visibility = Visibility.Collapsed;
                AnswersScroll.Visibility = Visibility.Visible;
                ShowAnswersError("Something went wrong, please try again");
            }
        }
        else
        {
            _firstNewPattern = null;
            _step = Step.DrawNewPattern;
            PatternStepTitle.Text = "Draw a new pattern";
            await FlashPatternError();
        }
    }

    private async Task FlashPatternError()
    {
        NewPatternControl.ShowError = true;
        await Task.Delay(500);
        NewPatternControl.ShowError = false;
    }

    private static bool PatternsEqual(List<int> a, List<int>? b)
    {
        if (b == null || a.Count != b.Count)
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

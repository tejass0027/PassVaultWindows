using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PassVaultWindows.Auth;

namespace PassVaultWindows.Views;

public partial class SecurityQuestionsSetupView : UserControl
{
    private readonly Func<List<string>, List<string>, Task> _onDone;

    public SecurityQuestionsSetupView(Func<List<string>, List<string>, Task> onDone)
    {
        InitializeComponent();
        _onDone = onDone;

        var suggested = SecurityQuestionManager.SuggestedQuestions;
        Question1Combo.ItemsSource = suggested;
        Question2Combo.ItemsSource = suggested;
        Question3Combo.ItemsSource = suggested;
        Question1Combo.SelectedIndex = 0;
        Question2Combo.SelectedIndex = 1;
        Question3Combo.SelectedIndex = 2;
    }

    private async void Continue_Click(object sender, RoutedEventArgs e)
    {
        var q1 = Question1Combo.SelectedItem as string ?? "";
        var q2 = Question2Combo.SelectedItem as string ?? "";
        var q3 = Question3Combo.SelectedItem as string ?? "";
        var a1 = Answer1Box.Text;
        var a2 = Answer2Box.Text;
        var a3 = Answer3Box.Text;

        if (string.IsNullOrWhiteSpace(a1) || string.IsNullOrWhiteSpace(a2) || string.IsNullOrWhiteSpace(a3))
        {
            ShowError("Please answer all three questions");
            return;
        }
        if (new HashSet<string> { q1, q2, q3 }.Count < 3)
        {
            ShowError("Please choose three different questions");
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        await _onDone(new List<string> { q1, q2, q3 }, new List<string> { a1, a2, a3 });
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}

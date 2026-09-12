using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PassVaultWindows.Data;

namespace PassVaultWindows.Views;

/// <summary>
/// The hidden vault's contents - a simple list of secret notes. Reachable only by drawing the
/// hidden vault's pattern on the login screen; there is no way to navigate here from the normal
/// vault or Settings.
/// </summary>
public partial class HiddenNotesView : UserControl
{
    private readonly AppState _appState;
    private readonly Action _onLock;
    private HiddenNote? _editing;

    public HiddenNotesView(AppState appState, Action onLock)
    {
        InitializeComponent();
        _appState = appState;
        _onLock = onLock;

        _appState.HiddenNotesRepository.NotesChanged += RefreshList;
        RefreshList();
    }

    private void RefreshList()
    {
        var items = _appState.HiddenNotesRepository.Notes
            .OrderByDescending(n => n.UpdatedAt)
            .Select(n => new NoteListItem(n))
            .ToList();
        NotesList.ItemsSource = items;
        EmptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NotesList.SelectedItem is NoteListItem item)
        {
            NotesList.SelectedItem = null;
            ShowEditor(item.Note);
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e) => ShowEditor(null);

    private void ShowEditor(HiddenNote? note)
    {
        _editing = note;
        EditTitleBox.Text = note?.Title ?? "";
        EditContentBox.Text = note?.Content ?? "";
        ListPanel.Visibility = Visibility.Collapsed;
        EditorPanel.Visibility = Visibility.Visible;
    }

    private async void SaveNote_Click(object sender, RoutedEventArgs e)
    {
        var note = _editing == null
            ? new HiddenNote { Title = EditTitleBox.Text, Content = EditContentBox.Text }
            : new HiddenNote
            {
                Id = _editing.Id,
                Title = EditTitleBox.Text,
                Content = EditContentBox.Text,
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        await _appState.HiddenNotesRepository.UpsertAsync(note);
        CloseEditor();
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e) => CloseEditor();

    private void CloseEditor()
    {
        _editing = null;
        EditorPanel.Visibility = Visibility.Collapsed;
        ListPanel.Visibility = Visibility.Visible;
    }

    private async void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: NoteListItem item })
        {
            var result = MessageBox.Show(
                $"\"{(string.IsNullOrEmpty(item.Note.Title) ? "(untitled)" : item.Note.Title)}\" will be permanently deleted.",
                "Delete this note?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _appState.HiddenNotesRepository.DeleteAsync(item.Note.Id);
            }
        }
    }

    private void Lock_Click(object sender, RoutedEventArgs e) => _onLock();

    private class NoteListItem
    {
        public HiddenNote Note { get; }
        public string Title => string.IsNullOrEmpty(Note.Title) ? "(untitled)" : Note.Title;
        public string Preview => Note.Content;

        public NoteListItem(HiddenNote note)
        {
            Note = note;
        }
    }
}

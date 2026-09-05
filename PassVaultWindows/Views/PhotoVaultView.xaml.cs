using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PassVaultWindows.Data;

namespace PassVaultWindows.Views;

public partial class PhotoVaultView : UserControl
{
    private readonly AppState _appState;
    private readonly Action _onBack;

    public PhotoVaultView(AppState appState, Action onBack)
    {
        InitializeComponent();
        _appState = appState;
        _onBack = onBack;

        _appState.PhotoVaultRepository.PhotosChanged += () => _ = RefreshAsync();
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var photos = _appState.PhotoVaultRepository.Photos.ToList();
        if (photos.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            PhotosGrid.ItemsSource = null;
            return;
        }
        EmptyText.Visibility = Visibility.Collapsed;

        var items = new List<PhotoItem>();
        foreach (var photo in photos)
        {
            var bytes = await _appState.PhotoVaultRepository.LoadPhotoBytesAsync(photo.Id);
            if (bytes == null)
            {
                continue;
            }
            items.Add(new PhotoItem(photo, DecodeImage(bytes)));
        }
        PhotosGrid.ItemsSource = items;
    }

    private static BitmapImage DecodeImage(byte[] bytes)
    {
        var image = new BitmapImage();
        using var stream = new MemoryStream(bytes);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private async void AddPhoto_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        LoadingBar.Visibility = Visibility.Visible;
        try
        {
            var bytes = await Task.Run(() => File.ReadAllBytes(dialog.FileName));
            await _appState.PhotoVaultRepository.AddPhotoAsync("", bytes);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't add that photo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            LoadingBar.Visibility = Visibility.Collapsed;
        }
    }

    private void Thumbnail_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PhotoItem item })
        {
            ShowViewer(item);
        }
    }

    private void ShowViewer(PhotoItem item)
    {
        var window = new Window
        {
            Title = "Photo",
            Owner = Window.GetWindow(this),
            Width = 500,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize
        };

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new Image { Source = item.Thumbnail, Stretch = Stretch.Uniform, MaxHeight = 500 });

        var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        var closeButton = new Button { Content = "Close", Style = (Style)FindResource("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
        closeButton.Click += (_, _) => window.Close();
        var deleteButton = new Button { Content = "Delete", Style = (Style)FindResource("SecondaryButtonStyle"), Foreground = (Brush)FindResource("ErrorBrush") };
        deleteButton.Click += async (_, _) =>
        {
            window.Close();
            await _appState.PhotoVaultRepository.DeletePhotoAsync(item.Photo.Id);
        };
        buttonRow.Children.Add(closeButton);
        buttonRow.Children.Add(deleteButton);
        panel.Children.Add(buttonRow);

        window.Content = panel;
        window.ShowDialog();
    }

    private void Back_Click(object sender, RoutedEventArgs e) => _onBack();

    private class PhotoItem
    {
        public VaultPhoto Photo { get; }
        public BitmapImage Thumbnail { get; }

        public PhotoItem(VaultPhoto photo, BitmapImage thumbnail)
        {
            Photo = photo;
            Thumbnail = thumbnail;
        }
    }
}

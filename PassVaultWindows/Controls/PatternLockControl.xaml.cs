using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PassVaultWindows.Controls;

/// <summary>
/// Classic 3x3 Android-style pattern lock, ported to WPF mouse events. Reports the dot
/// sequence (indices 0-8, left-to-right top-to-bottom) via <see cref="PatternCompleted"/> once
/// the mouse button is released. Set <see cref="ShowError"/> briefly (e.g. ~400ms) to flash the
/// last-drawn pattern red after a wrong attempt.
/// </summary>
public partial class PatternLockControl : UserControl
{
    public event Action<List<int>>? PatternCompleted;

    private readonly List<int> _selected = new();
    private bool _isDragging;
    private Point _currentPoint;
    private bool _showError;

    public static readonly DependencyProperty ShowErrorProperty = DependencyProperty.Register(
        nameof(ShowError), typeof(bool), typeof(PatternLockControl),
        new PropertyMetadata(false, OnShowErrorChanged));

    public bool ShowError
    {
        get => (bool)GetValue(ShowErrorProperty);
        set => SetValue(ShowErrorProperty, value);
    }

    private static void OnShowErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PatternLockControl)d;
        control._showError = (bool)e.NewValue;
        control.Redraw();
    }

    public PatternLockControl()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Redraw();
    }

    private List<Point> DotCenters()
    {
        double side = Math.Min(ActualWidth, ActualHeight);
        double margin = side * 0.18;
        double step = (side - 2 * margin) / 2.0;
        double offsetX = (ActualWidth - side) / 2.0;
        double offsetY = (ActualHeight - side) / 2.0;
        var centers = new List<Point>();
        for (int i = 0; i < 9; i++)
        {
            int row = i / 3;
            int col = i % 3;
            centers.Add(new Point(offsetX + margin + col * step, offsetY + margin + row * step));
        }
        return centers;
    }

    private int? NearestDotIndex(Point position)
    {
        var centers = DotCenters();
        double side = Math.Min(ActualWidth, ActualHeight);
        double touchRadius = side * 0.16;
        int? closestIndex = null;
        double closestDistance = double.MaxValue;
        for (int i = 0; i < centers.Count; i++)
        {
            double dx = position.X - centers[i].X;
            double dy = position.Y - centers[i].Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance < touchRadius && distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    private void DrawCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _selected.Clear();
        var pos = e.GetPosition(DrawCanvas);
        var idx = NearestDotIndex(pos);
        if (idx != null)
        {
            _selected.Add(idx.Value);
        }
        _currentPoint = pos;
        _isDragging = true;
        DrawCanvas.CaptureMouse();
        Redraw();
    }

    private void DrawCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }
        var pos = e.GetPosition(DrawCanvas);
        _currentPoint = pos;
        var idx = NearestDotIndex(pos);
        if (idx != null && !_selected.Contains(idx.Value))
        {
            _selected.Add(idx.Value);
        }
        Redraw();
    }

    private void DrawCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }
        _isDragging = false;
        DrawCanvas.ReleaseMouseCapture();
        var finished = new List<int>(_selected);
        _selected.Clear();
        Redraw();
        if (finished.Count > 0)
        {
            PatternCompleted?.Invoke(finished);
        }
    }

    private void Redraw()
    {
        DrawCanvas.Children.Clear();
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var centers = DotCenters();
        var accentBrush = (Brush)(TryFindResource("PrimaryBrush") ?? Brushes.MediumPurple);
        var dotBrush = (Brush)(TryFindResource("OutlineBrush") ?? Brushes.Gray);
        var errorBrush = Brushes.Crimson;
        var activeBrush = _showError ? errorBrush : accentBrush;

        double side = Math.Min(ActualWidth, ActualHeight);
        double dotRadius = side * 0.035;
        double ringRadius = dotRadius * 2.2;

        for (int i = 0; i < _selected.Count - 1; i++)
        {
            DrawLine(centers[_selected[i]], centers[_selected[i + 1]], activeBrush);
        }
        if (_isDragging && _selected.Count > 0)
        {
            DrawLine(centers[_selected[^1]], _currentPoint, activeBrush);
        }

        for (int i = 0; i < centers.Count; i++)
        {
            bool isSelected = _selected.Contains(i);
            if (isSelected)
            {
                DrawCircle(centers[i], ringRadius, WithAlpha(activeBrush, 0.18));
            }
            DrawCircle(centers[i], dotRadius, isSelected ? activeBrush : dotBrush);
        }
    }

    private void DrawLine(Point start, Point end, Brush brush)
    {
        var line = new Line
        {
            X1 = start.X,
            Y1 = start.Y,
            X2 = end.X,
            Y2 = end.Y,
            Stroke = brush,
            StrokeThickness = 6,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        DrawCanvas.Children.Add(line);
    }

    private void DrawCircle(Point center, double radius, Brush brush)
    {
        var ellipse = new Ellipse { Width = radius * 2, Height = radius * 2, Fill = brush };
        Canvas.SetLeft(ellipse, center.X - radius);
        Canvas.SetTop(ellipse, center.Y - radius);
        DrawCanvas.Children.Add(ellipse);
    }

    private static Brush WithAlpha(Brush brush, double alpha)
    {
        if (brush is SolidColorBrush solid)
        {
            var c = solid.Color;
            var faded = new SolidColorBrush(Color.FromArgb((byte)(alpha * 255), c.R, c.G, c.B));
            faded.Freeze();
            return faded;
        }
        return brush;
    }
}

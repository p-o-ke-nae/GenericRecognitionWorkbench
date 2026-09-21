using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Recognition.Core;

namespace Recognition.Wpf;

public sealed class ScreenRegionSelectionWindow : Window
{
    private readonly Canvas overlayCanvas;
    private readonly Rectangle selectionRectangle;
    private Point? dragStart;

    public ScreenRegionSelectionWindow()
    {
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
        Topmost = true;
        ShowInTaskbar = false;
        Cursor = Cursors.Cross;

        overlayCanvas = new Canvas();
        selectionRectangle = new Rectangle
        {
            Stroke = Brushes.DeepSkyBlue,
            StrokeThickness = 2,
            Fill = new SolidColorBrush(Color.FromArgb(50, 30, 144, 255)),
            Visibility = Visibility.Collapsed
        };

        overlayCanvas.Children.Add(selectionRectangle);
        Content = overlayCanvas;

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        KeyDown += OnKeyDown;
    }

    public RoiArea? SelectedRegion { get; private set; }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        dragStart = e.GetPosition(overlayCanvas);
        selectionRectangle.Visibility = Visibility.Visible;
        Canvas.SetLeft(selectionRectangle, dragStart.Value.X);
        Canvas.SetTop(selectionRectangle, dragStart.Value.Y);
        selectionRectangle.Width = 0;
        selectionRectangle.Height = 0;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (dragStart is null)
        {
            return;
        }

        var current = e.GetPosition(overlayCanvas);
        var left = Math.Min(dragStart.Value.X, current.X);
        var top = Math.Min(dragStart.Value.Y, current.Y);
        var width = Math.Abs(current.X - dragStart.Value.X);
        var height = Math.Abs(current.Y - dragStart.Value.Y);

        Canvas.SetLeft(selectionRectangle, left);
        Canvas.SetTop(selectionRectangle, top);
        selectionRectangle.Width = width;
        selectionRectangle.Height = height;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (dragStart is null)
        {
            return;
        }

        ReleaseMouseCapture();
        var left = Canvas.GetLeft(selectionRectangle);
        var top = Canvas.GetTop(selectionRectangle);
        var width = selectionRectangle.Width;
        var height = selectionRectangle.Height;
        dragStart = null;

        if (width >= 2 && height >= 2)
        {
            SelectedRegion = new RoiArea(
                (int)Math.Round(left + Left),
                (int)Math.Round(top + Top),
                (int)Math.Round(width),
                (int)Math.Round(height));
            DialogResult = true;
        }
        else
        {
            DialogResult = false;
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
        }
    }
}

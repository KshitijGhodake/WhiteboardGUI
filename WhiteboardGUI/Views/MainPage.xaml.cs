using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using WhiteboardGUI.Models;
using WhiteboardGUI.ViewModel;
using System;
using System.Collections.Generic;

namespace WhiteboardGUI.Views
{
    public partial class MainPage : Page
    {
        private MainPageViewModel ViewModel => DataContext as MainPageViewModel;
        private IShape _resizingShape;
        private string _currentHandle;
        private Point _startPoint;
        private Rect _initialBounds;
        private List<Point> _initialPoints;

        // Variables for Rotation
        private bool _isRotating = false;
        private double _initialAngle;
        private Point _rotationOrigin;

        public MainPage()
        {
            InitializeComponent();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel?.CanvasMouseDownCommand.Execute(e);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            ViewModel?.CanvasMouseMoveCommand.Execute(e);
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ViewModel?.CanvasMouseUpCommand.Execute(e);
        }

        private void PaletteToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            ColorPopup.IsOpen = true;
        }

        private void PaletteToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ColorPopup.IsOpen = false;
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            UploadPopup.IsOpen = true;
        }

        private void SubmitFileName_Click(object sender, RoutedEventArgs e)
        {
            UploadPopup.IsOpen = false;
            MessageBox.Show($"Filename '{ViewModel.SnapShotFileName}' has been set.", "Filename Set", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var rect = sender as FrameworkElement;
            if (rect != null)
            {
                _currentHandle = rect.Tag as string;
                _resizingShape = rect.DataContext as IShape;
                if (_resizingShape != null)
                {
                    _startPoint = e.GetPosition(this);
                    Mouse.Capture(rect);

                    // Store initial bounds and points for scaling
                    _initialBounds = _resizingShape.GetBounds();
                    if (_resizingShape is ScribbleShape scribble)
                    {
                        _initialPoints = new List<Point>(scribble.Points);
                    }

                    e.Handled = true;
                }
            }
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_resizingShape != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this);
                Vector totalDelta = currentPoint - _startPoint;
                ResizeShape(_resizingShape, _currentHandle, currentPoint);

                // Conditionally update _startPoint only for CircleShape and LineShape
                if (!(_resizingShape is ScribbleShape))
                {
                    _startPoint = currentPoint;
                }

                e.Handled = true;
            }
        }

        private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_resizingShape != null)
            {
                Mouse.Capture(null);
                var viewModel = this.DataContext as MainPageViewModel;
                if (viewModel != null)
                {
                    viewModel._renderingService.RenderShape(_resizingShape, "MODIFY");
                }
                _resizingShape = null;
                _currentHandle = null;
                _initialBounds = Rect.Empty;
                _initialPoints = null;
                e.Handled = true;
            }
        }

        private void ResizeShape(IShape shape, string handle, Point currentPoint)
        {
            if (shape is LineShape line)
            {
                ResizeLineShape(line, handle, currentPoint);
            }
            else if (shape is CircleShape circle)
            {
                ResizeCircleShape(circle, handle, currentPoint - _startPoint);
                _startPoint = currentPoint; // Update for CircleShape
            }
            else if (shape is ScribbleShape scribble)
            {
                ResizeScribbleShape(scribble, handle, currentPoint - _startPoint);
                // Do not update _startPoint for ScribbleShape
            }
            // Add more shapes if needed
        }

        private void ResizeLineShape(LineShape line, string handle, Point currentPoint)
        {
            if (handle == "Start")
            {
                line.StartX = currentPoint.X;
                line.StartY = currentPoint.Y;
            }
            else if (handle == "End")
            {
                line.EndX = currentPoint.X;
                line.EndY = currentPoint.Y;
            }
        }


           

        // Existing methods for other shapes remain unchanged


        private void ResizeScribbleShape(ScribbleShape scribble, string handle, Vector totalDelta)
                {
                    if (_initialBounds == Rect.Empty || _initialPoints == null)
                        return;

                    double minWidth = 8;
                    double minHeight = 8;

                    double newLeft = _initialBounds.Left;
                    double newTop = _initialBounds.Top;
                    double newWidth = _initialBounds.Width;
                    double newHeight = _initialBounds.Height;

                    // Adjust new bounds based on handle and totalDelta
                    switch (handle)
                    {
                        case "TopLeft":
                            newLeft += totalDelta.X;
                            newTop += totalDelta.Y;
                            newWidth -= totalDelta.X;
                            newHeight -= totalDelta.Y;
                            break;
                        case "TopRight":
                            newTop += totalDelta.Y;
                            newWidth += totalDelta.X;
                            newHeight -= totalDelta.Y;
                            break;
                        case "BottomLeft":
                            newLeft += totalDelta.X;
                            newWidth -= totalDelta.X;
                            newHeight += totalDelta.Y;
                            break;
                        case "BottomRight":
                            newWidth += totalDelta.X;
                            newHeight += totalDelta.Y;
                            break;
                        default:
                            break;
                    }

                    // Enforce minimum size
                    if (newWidth < minWidth)
                    {
                        if (handle == "TopLeft" || handle == "BottomLeft")
                        {
                            newLeft = _initialBounds.Right - minWidth;
                        }
                        newWidth = minWidth;
                    }

                    if (newHeight < minHeight)
                    {
                        if (handle == "TopLeft" || handle == "TopRight")
                        {
                            newTop = _initialBounds.Bottom - minHeight;
                        }
                        newHeight = minHeight;
                    }

                    // Determine the anchor point based on handle
                    Point anchor;
                    switch (handle)
                    {
                        case "TopLeft":
                            anchor = new Point(_initialBounds.Right, _initialBounds.Bottom);
                            break;
                        case "TopRight":
                            anchor = new Point(_initialBounds.Left, _initialBounds.Bottom);
                            break;
                        case "BottomLeft":
                            anchor = new Point(_initialBounds.Right, _initialBounds.Top);
                            break;
                        case "BottomRight":
                            anchor = new Point(_initialBounds.Left, _initialBounds.Top);
                            break;
                        default:
                            anchor = new Point(_initialBounds.Left, _initialBounds.Top);
                            break;
                    }

                    // Calculate scaling factors
                    double scaleX = _initialBounds.Width != 0 ? newWidth / _initialBounds.Width : 1;
                    double scaleY = _initialBounds.Height != 0 ? newHeight / _initialBounds.Height : 1;

                    // Apply scaling to each point relative to the anchor
                    List<Point> newPoints = new List<Point>();
                    foreach (var point in _initialPoints)
                    {
                        double newX = anchor.X + (point.X - anchor.X) * scaleX;
                        double newY = anchor.Y + (point.Y - anchor.Y) * scaleY;
                        newPoints.Add(new Point(newX, newY));
                    }

                    // Update the ScribbleShape's points
                    scribble.Points = newPoints;
                }
        private void ResizeCircleShape(CircleShape circle, string handle, Vector delta)
        {
            double minSize = 8; // Minimum size to prevent collapsing

            switch (handle)
            {
                case "TopLeft":
                    {
                        double newLeft = circle.Left + delta.X;
                        double newTop = circle.Top + delta.Y;
                        double newWidth = circle.Width - delta.X;
                        double newHeight = circle.Height - delta.Y;

                        if (newWidth >= minSize && newHeight >= minSize)
                        {
                            circle.CenterX = newLeft + newWidth / 2;
                            circle.CenterY = newTop + newHeight / 2;
                            circle.RadiusX = newWidth / 2;
                            circle.RadiusY = newHeight / 2;
                        }
                        break;
                    }
                case "TopRight":
                    {
                        double newTop = circle.Top + delta.Y;
                        double newWidth = circle.Width + delta.X;
                        double newHeight = circle.Height - delta.Y;

                        if (newWidth >= minSize && newHeight >= minSize)
                        {
                            circle.CenterX = circle.Left + newWidth / 2;
                            circle.CenterY = newTop + newHeight / 2;
                            circle.RadiusX = newWidth / 2;
                            circle.RadiusY = newHeight / 2;
                        }
                        break;
                    }
                case "BottomLeft":
                    {
                        double newLeft = circle.Left + delta.X;
                        double newWidth = circle.Width - delta.X;
                        double newHeight = circle.Height + delta.Y;

                        if (newWidth >= minSize && newHeight >= minSize)
                        {
                            circle.CenterX = newLeft + newWidth / 2;
                            circle.CenterY = circle.Top + newHeight / 2;
                            circle.RadiusX = newWidth / 2;
                            circle.RadiusY = newHeight / 2;
                        }
                        break;
                    }
                case "BottomRight":
                    {
                        double newWidth = circle.Width + delta.X;
                        double newHeight = circle.Height + delta.Y;

                        if (newWidth >= minSize && newHeight >= minSize)
                        {
                            circle.CenterX = circle.Left + newWidth / 2;
                            circle.CenterY = circle.Top + newHeight / 2;
                            circle.RadiusX = newWidth / 2;
                            circle.RadiusY = newHeight / 2;
                        }
                        break;
                    }
                default:
                    break;
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

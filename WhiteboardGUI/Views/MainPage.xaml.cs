using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using WhiteboardGUI.Models;
using WhiteboardGUI.ViewModel;

namespace WhiteboardGUI.Views
{
    public partial class MainPage : Page
    {
        private MainPageViewModel ViewModel => DataContext as MainPageViewModel;
        private IShape _resizingShape;
        private string _currentHandle;
        private Point _startPoint;
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

        // Event handler for ToggleButton Unchecked
        private void PaletteToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ColorPopup.IsOpen = false;
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            UploadPopup.IsOpen = true;
        }

        // Event handler for Submit Button Click in Upload Popup
        private void SubmitFileName_Click(object sender, RoutedEventArgs e)
        {
            // Close the Popup
            UploadPopup.IsOpen = false;

            // Optionally, perform actions with the filename
            // For example, validate the filename or trigger a save operation

            MessageBox.Show($"Filename '{ViewModel.SnapShotFileName}' has been set.", "Filename Set", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var rect = sender as Rectangle;
            if (rect != null)
            {
                _currentHandle = rect.Tag as string;
                _resizingShape = rect.DataContext as IShape;
                if (_resizingShape != null)
                {
                    _startPoint = e.GetPosition(null);
                    Mouse.Capture(rect);
                    e.Handled = true;
                }
            }
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_resizingShape != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(null);
                Vector delta = currentPoint - _startPoint;
                ResizeShape(_resizingShape, _currentHandle, delta);
                _startPoint = currentPoint;
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
                    viewModel.RenderShape(_resizingShape, "MODIFY");
                }
                _resizingShape = null;
                _currentHandle = null;
                e.Handled = true;
            }
        }
        private void ResizeShape(IShape shape, string handle, Vector delta)
        {
            if (shape is CircleShape circle)
            {
                ResizeCircleShape(circle, handle, delta);
            }
            //else if (shape is LineShape line)
            //{
            //    ResizeLineShape(line, handle, delta);
            //}
            // Handle other shapes if necessary
        }
        private void ResizeCircleShape(CircleShape circle, string handle, Vector delta)
        {
            double minSize = 5; // Minimum size to prevent collapsing
            switch (handle)
            {
                case "TopLeft":
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
                    // Implement other cases for different handles
            }
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WhiteboardGUI.ViewModel;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Views
{
    public partial class MainPage : Page
    {
        private MainPageViewModel ViewModel => DataContext as MainPageViewModel;

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

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // You can implement any key handling for the text input here if needed
        }

        private void Shape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel != null)
            {
                if (ViewModel.CurrentTool == ShapeType.Select)
                {
                    // Prevent event from bubbling up to the Canvas
                    e.Handled = true;

                    // Get the shape associated with this item
                    if (sender is FrameworkElement element && element.DataContext is IShape shape)
                    {
                        ViewModel.SelectShapeCommand.Execute(shape);
                        ViewModel.CanvasMouseDownCommand.Execute(e);
                    }
                }
            }
        }
    }
}

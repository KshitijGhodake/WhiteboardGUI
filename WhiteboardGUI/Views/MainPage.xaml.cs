using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WhiteboardGUI.ViewModel;

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
    }
}

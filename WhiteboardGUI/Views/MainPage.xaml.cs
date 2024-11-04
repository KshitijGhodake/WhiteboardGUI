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
    }
}

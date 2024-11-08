using System.Windows;
using WhiteboardGUI.Views; // Ensure this namespace is included

namespace WhiteboardGUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainPage mainPage = new MainPage();
            this.Content = mainPage;
        }

    }
}

using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WhiteboardGUI
{
    /// <summary>
    /// Mouse tracking logic.
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum Tool { Pencil, Line, Circle }
        private Tool currentTool = Tool.Pencil;
        private Point startPoint;
        private Line currentLine;
        private Ellipse currentEllipse;
        private Polyline currentPolyline;
        private List<Shape> shapes = new List<Shape>(); 
        private Brush selectedColor = Brushes.Black;

        public MainWindow()
        {
            InitializeComponent();
            drawingCanvas.MouseDown += Canvas_MouseDown;
            drawingCanvas.MouseMove += Canvas_MouseMove;
            drawingCanvas.MouseUp += Canvas_MouseUp;
        }

        private void Pencil_Click(object sender, RoutedEventArgs e)
        {
            currentTool = Tool.Pencil;
        }

        private void Line_Click(object sender, RoutedEventArgs e)
        {
            currentTool = Tool.Line;
        }

        private void Circle_Click(object sender, RoutedEventArgs e)
        {
            currentTool = Tool.Circle;
        }

        private void ColorPicker_SelectionChanged(object sender, RoutedEventArgs e)
        {
            string selectedColorName = (colorPicker.SelectedItem as ComboBoxItem)?.Content.ToString();
            selectedColor = selectedColorName switch
            {
                "Red" => Brushes.Red,
                "Blue" => Brushes.Blue,
                "Green" => Brushes.Green,
                _ => Brushes.Black
            };
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(drawingCanvas);

            switch (currentTool)
            {
                case Tool.Pencil:
                    currentPolyline = new Polyline
                    {
                        Stroke = selectedColor,
                        StrokeThickness = 2,
                    };
                    currentPolyline.Points.Add(startPoint);
                    drawingCanvas.Children.Add(currentPolyline);
                    shapes.Add(currentPolyline);
                    break;
                case Tool.Line:
                    currentLine = new Line
                    {
                        Stroke = selectedColor,
                        StrokeThickness = 2,
                        X1 = startPoint.X,
                        Y1 = startPoint.Y
                    };
                    drawingCanvas.Children.Add(currentLine);
                    shapes.Add(currentLine);
                    break;
                case Tool.Circle:
                    currentEllipse = new Ellipse
                    {
                        Stroke = selectedColor,
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(currentEllipse, startPoint.X);
                    Canvas.SetTop(currentEllipse, startPoint.Y);
                    drawingCanvas.Children.Add(currentEllipse);
                    shapes.Add(currentEllipse);
                    break;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point endPoint = e.GetPosition(drawingCanvas);

                switch (currentTool)
                {
                    case Tool.Pencil:
                        currentPolyline?.Points.Add(endPoint);
                        break;
                    case Tool.Line:
                        if (currentLine != null)
                        {
                            currentLine.X2 = endPoint.X;
                            currentLine.Y2 = endPoint.Y;
                        }
                        break;
                    case Tool.Circle:
                        if (currentEllipse != null)
                        {
                            double radiusX = Math.Abs(endPoint.X - startPoint.X);
                            double radiusY = Math.Abs(endPoint.Y - startPoint.Y);
                            currentEllipse.Width = 2 * radiusX;
                            currentEllipse.Height = 2 * radiusY;
                            Canvas.SetLeft(currentEllipse, Math.Min(startPoint.X, endPoint.X));
                            Canvas.SetTop(currentEllipse, Math.Min(startPoint.Y, endPoint.Y));
                        }
                        break;
                }
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            currentLine = null;
            currentEllipse = null;
            currentPolyline = null;
        }
    }
}
using System.Windows;
using System.Windows.Media;

namespace WhiteboardGUI.Models
{
    public class CircleShape : ShapeBase
    {
        public override string ShapeType => "Circle";

        // Existing properties
        private double _centerX;
        private double _centerY;
        private double _radiusX;
        private double _radiusY;

        public double CenterX
        {
            get => _centerX;
            set { _centerX = value; OnPropertyChanged(nameof(CenterX)); OnPropertyChanged(nameof(Left)); }
        }

        public double CenterY
        {
            get => _centerY;
            set { _centerY = value; OnPropertyChanged(nameof(CenterY)); OnPropertyChanged(nameof(Top)); }
        }

        public double RadiusX
        {
            get => _radiusX;
            set { _radiusX = value; OnPropertyChanged(nameof(RadiusX)); OnPropertyChanged(nameof(Width)); }
        }

        public double RadiusY
        {
            get => _radiusY;
            set { _radiusY = value; OnPropertyChanged(nameof(RadiusY)); OnPropertyChanged(nameof(Height)); }
        }

        // Properties for binding in XAML
        public double Left => CenterX;
        public double Top => CenterY;
        public double Width => 2 * RadiusX;
        public double Height => 2 * RadiusY;
        public Brush Stroke => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
    }
}

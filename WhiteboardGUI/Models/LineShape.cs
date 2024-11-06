using System.Windows.Media;

namespace WhiteboardGUI.Models
{
    public class LineShape : ShapeBase
    {
        public override string ShapeType => "Line";

        private double _startX;
        private double _startY;
        private double _endX;
        private double _endY;

        public double StartX
        {
            get => _startX;
            set { _startX = value; OnPropertyChanged(nameof(StartX)); }
        }

       

        public double StartY
        {
            get => _startY;
            set { _startY = value; OnPropertyChanged(nameof(StartY)); }
        }

        public double EndX
        {
            get => _endX;
            set { _endX = value; OnPropertyChanged(nameof(EndX)); }
        }

        public double EndY
        {
            get => _endY;
            set { _endY = value; OnPropertyChanged(nameof(EndY)); }
        }

        // Property for binding in XAML
        public Brush Stroke => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
    }
}

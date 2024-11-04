using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace WhiteboardGUI.Models
{
    public class ScribbleShape : ShapeBase
    {
        public override string ShapeType => "Scribble";

        private List<Point> _points = new List<Point>();

        public List<Point> Points
        {
            get => _points;
            set
            {
                _points = value;
                OnPropertyChanged(nameof(Points));
                OnPropertyChanged(nameof(PointCollection));
            }
        }

        // Property for binding in XAML
        public PointCollection PointCollection => new PointCollection(Points);

        public Brush Stroke
        {
            get
            {
                var color = (Color)ColorConverter.ConvertFromString(Color);
                return new SolidColorBrush(color);
            }
        }
    }
}

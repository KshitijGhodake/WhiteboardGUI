using System.Windows;
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
            set
            {
                _startX = value;
                OnPropertyChanged(nameof(StartX));
                OnPropertyChanged(nameof(Left));
                OnPropertyChanged(nameof(Width));
            }
        }



        public double StartY
        {
            get => _startY;
            set
            {
                _startY = value;
                OnPropertyChanged(nameof(StartY));
                OnPropertyChanged(nameof(Top));
                OnPropertyChanged(nameof(Height));
            }
        }

        public double EndX
        {
            get => _endX;
            set
            {
                _endX = value;
                OnPropertyChanged(nameof(EndX));
                OnPropertyChanged(nameof(Left));
                OnPropertyChanged(nameof(Width));
            }
        }

        public double EndY
        {
            get => _endY;
            set
            {
                _endY = value;
                OnPropertyChanged(nameof(EndY));
                OnPropertyChanged(nameof(Top));
                OnPropertyChanged(nameof(Height));
            }
        }

        public double Left => Math.Min(StartX, EndX);
        public double Top => Math.Min(StartY, EndY);
        public double Width => Math.Abs(EndX - StartX);
        public double Height => Math.Abs(EndY - StartY);



        // Property for binding in XAML
        public Brush Stroke => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));

        public override Rect GetBounds()
        {
            return new Rect(Left, Top, Width, Height);
        }

        public override IShape Clone()
        {
            return new LineShape
            {
                ShapeId = this.ShapeId, // Assign a new unique ID
                UserID = this.UserID,
                Color = this.Color,
                StrokeThickness = this.StrokeThickness,
                LastModifierID = this.LastModifierID,
                IsSelected = false, // New shape should not be selected by default
                StartX = this.StartX,
                StartY = this.StartY,
                EndX = this.EndX,
                EndY = this.EndY
                // If ShapeBase has additional properties, copy them here
            };
        }
    }
}
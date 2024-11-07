using System;
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

        // Properties for binding in XAML
        public double Left => Math.Min(StartX, EndX);
        public double Top => Math.Min(StartY, EndY);
        public double Width => Math.Abs(EndX - StartX);
        public double Height => Math.Abs(EndY - StartY);

        public Brush Stroke => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));

        // Implement the GetBounds method
        public override Rect GetBounds()
        {
            return new Rect(Left, Top, Width, Height);
        }
    }
}

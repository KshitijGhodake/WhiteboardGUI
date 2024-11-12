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
                if (_startX != value)
                {
                    _startX = value;
                    OnPropertyChanged(nameof(StartX));
                    OnCoordinateChanged();
                    OnPropertyChanged(nameof(Left));
                    OnPropertyChanged(nameof(Width));
                    OnPropertyChanged(nameof(RelativeStartX));
                    // When StartX changes, RelativeEndX might change if Left changes
                    OnPropertyChanged(nameof(RelativeEndX));
                    OnPropertyChanged(nameof(Bottomleft));
                }
            }
        }

        public double StartY
        {
            get => _startY;
            set
            {
                if (_startY != value)
                {
                    _startY = value;
                    OnPropertyChanged(nameof(StartY));
                    OnCoordinateChanged();
                    OnPropertyChanged(nameof(Top));
                    OnPropertyChanged(nameof(Height));
                    OnPropertyChanged(nameof(RelativeStartY));
                    OnPropertyChanged(nameof(RelativeEndY));
                    OnPropertyChanged(nameof(Bottomleft));
                }
            }
        }

        public double EndX
        {
            get => _endX;
            set
            {
                if (_endX != value)
                {
                    _endX = value;
                    OnPropertyChanged(nameof(EndX));
                    OnCoordinateChanged();
                    OnPropertyChanged(nameof(Left));
                    OnPropertyChanged(nameof(Width));
                    OnPropertyChanged(nameof(RelativeEndX));
                    OnPropertyChanged(nameof(RelativeStartX));
                    OnPropertyChanged(nameof(Bottomleft));
                }
            }
        }

        public double EndY
        {
            get => _endY;
            set
            {
                if (_endY != value)
                {
                    _endY = value;
                    OnPropertyChanged(nameof(EndY));
                    OnCoordinateChanged();
                    OnPropertyChanged(nameof(Top));
                    OnPropertyChanged(nameof(Height));
                    OnPropertyChanged(nameof(RelativeEndY));
                    OnPropertyChanged(nameof(RelativeStartY));
                    OnPropertyChanged(nameof(Bottomleft));
                }
            }
        }

        private void OnCoordinateChanged()
        {
            OnPropertyChanged(nameof(MidX));
            OnPropertyChanged(nameof(MidY));
            OnPropertyChanged(nameof(Left));
            OnPropertyChanged(nameof(Top));
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
            OnPropertyChanged(nameof(StartHandleX));
            OnPropertyChanged(nameof(StartHandleY));
            OnPropertyChanged(nameof(EndHandleX));
            OnPropertyChanged(nameof(EndHandleY));
        }

        public double MidX => (StartX + EndX) / 2;
        public double MidY => (StartY + EndY) / 2;

        // Property for binding in XAML
        public Brush Stroke => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));

        public double Left => Math.Min(StartX, EndX) - HandleSize / 2;
        public double Top => Math.Min(StartY, EndY) - HandleSize / 2;
        public double Width => Math.Abs(EndX - StartX) + HandleSize;
        public double Height => Math.Abs(EndY - StartY) + HandleSize;

        public double HandleSize => 8;

        // Properties for handle positions
        public double StartHandleX => StartX - HandleSize / 2;
        public double StartHandleY => StartY - HandleSize / 2;
        public double EndHandleX => EndX - HandleSize / 2;
        public double EndHandleY => EndY - HandleSize / 2;

        public double RelativeStartX => StartX - Left;
        public double RelativeStartY => StartY - Top - Height;
        public double RelativeEndX => EndX - Left;
        public double RelativeEndY => EndY - Top - Height;

        public double Bottomleft => Top + Height;


        public override Rect GetBounds()
        {
            // Return the axis-aligned bounding box of the line
            return new Rect(Left, Top, Width, Height);
        }

        public override IShape Clone()
        {
            return new LineShape
            {
                ShapeId = this.ShapeId,
                UserID = this.UserID,
                Color = this.Color,
                StrokeThickness = this.StrokeThickness,
                LastModifierID = this.LastModifierID,
                IsSelected = false,
                StartX = this.StartX,
                StartY = this.StartY,
                EndX = this.EndX,
                EndY = this.EndY,
                // RotationAngle property removed
            };
        }
    }
}

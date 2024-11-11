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
                }
            }
        }

        private void OnCoordinateChanged()
        {
            OnPropertyChanged(nameof(Left));
            OnPropertyChanged(nameof(Top));
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
            OnPropertyChanged(nameof(MidX));
            OnPropertyChanged(nameof(MidY));
            OnPropertyChanged(nameof(StartHandleXRotated));
            OnPropertyChanged(nameof(StartHandleYRotated));
            OnPropertyChanged(nameof(EndHandleXRotated));
            OnPropertyChanged(nameof(EndHandleYRotated));
            OnPropertyChanged(nameof(RotatedStartPoint));
            OnPropertyChanged(nameof(RotatedEndPoint));
            OnPropertyChanged(nameof(RotatedLeft));
            OnPropertyChanged(nameof(RotatedTop));
            OnPropertyChanged(nameof(RotatedWidth));
            OnPropertyChanged(nameof(RotatedHeight));
            OnPropertyChanged(nameof(RotationHandleXRotated));
            OnPropertyChanged(nameof(RotationHandleYRotated));
        }

        public double Left => Math.Min(StartX, EndX);
        public double Top => Math.Min(StartY, EndY);
        public double Width => Math.Abs(EndX - StartX);
        public double Height => Math.Abs(EndY - StartY);
        public double HandleSize => 8;

        public double MidX => (StartX + EndX) / 2;
        public double MidY => (StartY + EndY) / 2;

        private double _rotationAngle;
        public double RotationAngle
        {
            get => _rotationAngle;
            set
            {
                if (_rotationAngle != value)
                {
                    _rotationAngle = value;
                    OnPropertyChanged(nameof(RotationAngle));
                    OnCoordinateChanged();
                }
            }
        }

        // Property for binding in XAML
        public Brush Stroke => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));

        public override Rect GetBounds()
        {
            // Return the axis-aligned bounding box of the rotated line
            return new Rect(RotatedLeft, RotatedTop, RotatedWidth, RotatedHeight);
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
                RotationAngle = this.RotationAngle,
                ZIndex = this.ZIndex
            };
        }

        // Helper method to rotate a point around (centerX, centerY) by angle degrees
        private Point RotatePoint(double x, double y, double centerX, double centerY, double angle)
        {
            double radians = angle * Math.PI / 180;
            double cosTheta = Math.Cos(radians);
            double sinTheta = Math.Sin(radians);
            double dx = x - centerX;
            double dy = y - centerY;
            double newX = centerX + (dx * cosTheta - dy * sinTheta);
            double newY = centerY + (dx * sinTheta + dy * cosTheta);
            return new Point(newX, newY);
        }

        // Rotated Start and End Points
        public Point RotatedStartPoint => RotatePoint(StartX, StartY, MidX, MidY, RotationAngle);
        public Point RotatedEndPoint => RotatePoint(EndX, EndY, MidX, MidY, RotationAngle);

        // Axis-aligned bounding box of rotated line
        public double RotatedLeft => Math.Min(RotatedStartPoint.X, RotatedEndPoint.X);
        public double RotatedTop => Math.Min(RotatedStartPoint.Y, RotatedEndPoint.Y);
        public double RotatedWidth => Math.Abs(RotatedEndPoint.X - RotatedStartPoint.X);
        public double RotatedHeight => Math.Abs(RotatedEndPoint.Y - RotatedStartPoint.Y);

        // Properties for handle positions
        public double StartHandleXRotated => RotatedStartPoint.X - HandleSize / 2;
        public double StartHandleYRotated => RotatedStartPoint.Y - HandleSize / 2;
        public double EndHandleXRotated => RotatedEndPoint.X - HandleSize / 2;
        public double EndHandleYRotated => RotatedEndPoint.Y - HandleSize / 2;

        // Unrotated rotation handle position
        public double RotationHandleXUnrotated => MidX;
        public double RotationHandleYUnrotated => MidY - 30;

        // Rotated rotation handle position
        public double RotationHandleXRotated => RotatePoint(RotationHandleXUnrotated, RotationHandleYUnrotated, MidX, MidY, RotationAngle).X - HandleSize / 2;
        public double RotationHandleYRotated => RotatePoint(RotationHandleXUnrotated, RotationHandleYUnrotated, MidX, MidY, RotationAngle).Y - HandleSize / 2;
    }
}

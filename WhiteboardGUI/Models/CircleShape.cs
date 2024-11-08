using System;
using System.Windows;
using System.Windows.Media;

namespace WhiteboardGUI.Models
{
    public class CircleShape : ShapeBase
    {
        public override string ShapeType => "Circle";

        private double _centerX;
        private double _centerY;
        private double _radiusX;
        private double _radiusY;
        public double HandleSize => 8;
        public double TopRightHandleX => Left + 2*_radiusX;
        public double DownLeftHandleY => Top + 2 * _radiusY;


        public double CenterX
        {
            get => _centerX;
            set
            {
                _centerX = value;
                OnPropertyChanged(nameof(CenterX));
                OnPropertyChanged(nameof(Left));
            }
        }

        public double CenterY
        {
            get => _centerY;
            set
            {
                _centerY = value;
                OnPropertyChanged(nameof(CenterY));
                OnPropertyChanged(nameof(Top));
            }
        }

        public double RadiusX
        {
            get => _radiusX;
            set
            {
                _radiusX = value;
                OnPropertyChanged(nameof(RadiusX));
                OnPropertyChanged(nameof(Left));
                OnPropertyChanged(nameof(Width));
            }
        }

        public double RadiusY
        {
            get => _radiusY;
            set
            {
                _radiusY = value;
                OnPropertyChanged(nameof(RadiusY));
                OnPropertyChanged(nameof(Top));
                OnPropertyChanged(nameof(Height));
            }
        }

    

        // Corrected properties for binding in XAML
        public double Left => CenterX - RadiusX;
        public double Top => CenterY - RadiusY;
        public double Width => 2 * RadiusX;
        public double Height => 2 * RadiusY;

        public Brush Stroke => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));

        // Implement the GetBounds method
        public override Rect GetBounds()
        {
            return new Rect(Left, Top, Width, Height);
        }

        public override IShape Clone()
        {
            return new CircleShape
            {
                ShapeId = this.ShapeId, // Assign a new unique ID
                UserID = this.UserID,
                Color = this.Color,
                StrokeThickness = this.StrokeThickness,
                LastModifierID = this.LastModifierID,
                IsSelected = this.IsSelected, // New shape should not be selected by default
                CenterX = this.CenterX,
                CenterY = this.CenterY,
                RadiusX = this.RadiusX,
                RadiusY = this.RadiusY
                

            };
        }
    }
}
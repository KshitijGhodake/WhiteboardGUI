using System;
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
                OnPropertyChanged(nameof(Left));
                OnPropertyChanged(nameof(Top));
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Height));
            }
        }


        // Property for binding in XAML
        public PointCollection PointCollection => new PointCollection(Points);

        public Brush Stroke => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));

        // Method to add a point and trigger UI update
        public void AddPoint(Point point)
        {
            _points.Add(point);
            OnPropertyChanged(nameof(PointCollection));
            OnPropertyChanged(nameof(Left));
            OnPropertyChanged(nameof(Top));
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
        }

        // Properties for binding in XAML
        public double Left => GetBounds().Left;
        public double Top => GetBounds().Top;
        public double Width => GetBounds().Width;
        public double Height => GetBounds().Height;

        // Implement the GetBounds method
        public override Rect GetBounds()
        {
            if (Points == null || Points.Count == 0)
                return Rect.Empty;

            double minX = Points.Min(p => p.X);
            double minY = Points.Min(p => p.Y);
            double maxX = Points.Max(p => p.X);
            double maxY = Points.Max(p => p.Y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public override IShape Clone()
        {
            return new ScribbleShape
            {
                ShapeId = this.ShapeId, // Assign a new unique ID
                UserID = this.UserID,
                Color = this.Color,
                StrokeThickness = this.StrokeThickness,
                LastModifierID = this.LastModifierID,
                IsSelected = false, // New shape should not be selected by default
                Points = new List<Point>(this.Points) // Create a deep copy of the points
            };
        }
    }
}
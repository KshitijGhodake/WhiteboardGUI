using System;
using System.Windows;
using System.Windows.Media;

namespace WhiteboardGUI.Models
{
    public class TextboxModel : ShapeBase
    {
        public override string ShapeType => "TextboxModel";

        private string _text;
        private double _width;
        private double _height;
        private double _x;
        private double _y;
        private double _fontSize;

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(nameof(Text)); }
        }

        public double Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(nameof(Width)); }
        }

        public double Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(nameof(Height)); }
        }

        public double X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(nameof(X)); OnPropertyChanged(nameof(Left)); }
        }

        public double Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(nameof(Y)); OnPropertyChanged(nameof(Top)); }
        }

        public double FontSize
        {
            get => _fontSize;
            set { _fontSize = value; OnPropertyChanged(nameof(FontSize)); }
        }

        // Properties for binding in XAML
        public double Left => X;
        public double Top => Y;

        public Brush Background => new SolidColorBrush(Colors.LightGray);
        public Brush BorderBrush => new SolidColorBrush(Colors.Blue);
        public Brush Foreground => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));

        // Implement IsSelected property
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        // Implement GetBounds method
        public override Rect GetBounds()
        {
            return new Rect(Left, Top, Width, Height);
        }

        public override IShape Clone()
        {
            return new TextboxModel
            {
                ShapeId = this.ShapeId, // Assign a new unique ID
                UserID = this.UserID,
                Color = this.Color,
                StrokeThickness = this.StrokeThickness,
                LastModifierID = this.LastModifierID,
                IsSelected = false, // New shape should not be selected by default
                Text = this.Text,
                Width = this.Width,
                Height = this.Height,
                X = this.X,
                Y = this.Y,
                FontSize = this.FontSize
                // Copy additional properties from ShapeBase if necessary
            };
        }

    }
}